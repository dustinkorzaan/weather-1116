using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;
using Core.Agent.Events;
using Core.Chat.Models;
using Core.Data.Domain;
using CQMediator;

namespace Core.Chat.Services;

/// <summary>
/// Decorates a Chat1a-Chat4b <see cref="IChatClientService"/> with dbo.AgentActivities logging.
/// Registered once per keyed service in <see cref="ChatServiceCollectionExtensions"/> so none of
/// the 7 chat tabs, their controllers, or their Core services need to know this exists.
///
/// Every tab's own tool_start/tool_end SSE events (already flowing through this decorator) are
/// also logged as nested Request/Response rows sharing the turn's RunId. For Chat4a/Chat4b,
/// those events are the orchestrator's delegation calls to its named sub-agents ("Geo",
/// "NonAIWeather" -- see docs/5-chat-clients/5-chat-clients.md), so featureCategory decides
/// whether the tool name is logged as AgentName (MultiAgent) or ToolName (everything else). Geo's
/// and NonAI Weather's own inner tool calls stay invisible here -- same nested blind spot the SSE
/// stream and usage chip already have (documented in 5-chat-clients.md).
///
/// The Request row is logged lazily, on the first event actually observed from the inner
/// service (always its "session" event, per every one of the 7 Chat*Service implementations),
/// so a brand-new chat's very first row gets the real assigned SessionId instead of an empty
/// string. If the inner enumeration throws before or during iteration -- missing Foundry env
/// vars, a network failure, a client disconnect -- this still logs a paired Response row with
/// ErrorMessage set (logging a Request row first if none was logged yet) before rethrowing, so
/// no turn is ever left as an unpaired Request. Log write failures themselves are not caught
/// here: they propagate and fail the turn, the same fail-closed policy as a missing
/// DB_CONNECTION_STRING at startup.
/// </summary>
public class AgentActivityLoggingChatClientService(
    IChatClientService inner,
    IMediator mediator,
    string feature,
    string featureCategory) : IChatClientService
{
    public async IAsyncEnumerable<ChatStreamEvent> SendMessageAsync(
        ChatSendMessageRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var runId = Guid.NewGuid();
        var sessionId = request.SessionId;
        Guid? correlationId = null;
        var responseText = new StringBuilder();
        var loopNumber = 0;
        var pendingToolCalls = new Dictionary<string, Queue<(Guid CorrelationId, int LoopNumber)>>();

        async Task<Guid> EnsureRequestLoggedAsync()
        {
            correlationId ??= await mediator.Send(new LogAgentActivityEvent
            {
                Direction = AgentActivityDirection.Request,
                RunId = runId,
                Feature = feature,
                FeatureCategory = featureCategory,
                SessionId = sessionId ?? string.Empty,
                Content = request.Message,
            }, cancellationToken);

            return correlationId.Value;
        }

        var enumerator = inner.SendMessageAsync(request, cancellationToken).GetAsyncEnumerator(cancellationToken);
        try
        {
            while (true)
            {
                ChatStreamEvent? streamEvent = null;
                ExceptionDispatchInfo? failure = null;
                try
                {
                    if (await enumerator.MoveNextAsync())
                    {
                        streamEvent = enumerator.Current;
                    }
                }
                catch (Exception ex)
                {
                    failure = ExceptionDispatchInfo.Capture(ex);
                }

                if (failure is not null)
                {
                    await EnsureRequestLoggedAsync();
                    await mediator.Send(new LogAgentActivityEvent
                    {
                        Direction = AgentActivityDirection.Response,
                        RunId = runId,
                        CorrelationId = correlationId,
                        Feature = feature,
                        FeatureCategory = featureCategory,
                        SessionId = sessionId ?? string.Empty,
                        Content = responseText.ToString(),
                        ErrorMessage = failure.SourceException.Message,
                    }, cancellationToken);
                    failure.Throw();
                }

                if (streamEvent is null)
                {
                    yield break;
                }

                if (streamEvent.Type == "session")
                {
                    sessionId = streamEvent.SessionId ?? sessionId;
                }

                await EnsureRequestLoggedAsync();

                switch (streamEvent.Type)
                {
                    case "token":
                        if (streamEvent.Text is not null)
                        {
                            responseText.Append(streamEvent.Text);
                        }
                        break;
                    case "tool_start":
                        if (streamEvent.ToolName is not null)
                        {
                            loopNumber++;
                            var toolCallId = await mediator.Send(new LogAgentActivityEvent
                            {
                                Direction = AgentActivityDirection.Request,
                                RunId = runId,
                                Feature = feature,
                                FeatureCategory = featureCategory,
                                SessionId = sessionId ?? string.Empty,
                                AgentName = featureCategory == AgentActivityFeatureCategory.MultiAgent ? streamEvent.ToolName : null,
                                ToolName = featureCategory == AgentActivityFeatureCategory.MultiAgent ? null : streamEvent.ToolName,
                                LoopNumber = loopNumber,
                                Content = streamEvent.ToolArguments,
                            }, cancellationToken);

                            if (!pendingToolCalls.TryGetValue(streamEvent.ToolName, out var queue))
                            {
                                queue = new Queue<(Guid, int)>();
                                pendingToolCalls[streamEvent.ToolName] = queue;
                            }

                            queue.Enqueue((toolCallId, loopNumber));
                        }
                        break;
                    case "tool_end":
                        if (streamEvent.ToolName is not null
                            && pendingToolCalls.TryGetValue(streamEvent.ToolName, out var pendingQueue)
                            && pendingQueue.TryDequeue(out var pending))
                        {
                            await mediator.Send(new LogAgentActivityEvent
                            {
                                Direction = AgentActivityDirection.Response,
                                RunId = runId,
                                CorrelationId = pending.CorrelationId,
                                Feature = feature,
                                FeatureCategory = featureCategory,
                                SessionId = sessionId ?? string.Empty,
                                AgentName = featureCategory == AgentActivityFeatureCategory.MultiAgent ? streamEvent.ToolName : null,
                                ToolName = featureCategory == AgentActivityFeatureCategory.MultiAgent ? null : streamEvent.ToolName,
                                LoopNumber = pending.LoopNumber,
                                Content = streamEvent.ToolResult,
                            }, cancellationToken);
                        }
                        break;
                    case "done":
                        await mediator.Send(new LogAgentActivityEvent
                        {
                            Direction = AgentActivityDirection.Response,
                            RunId = runId,
                            CorrelationId = correlationId,
                            Feature = feature,
                            FeatureCategory = featureCategory,
                            SessionId = sessionId ?? string.Empty,
                            Content = responseText.ToString(),
                            InputTokenCount = streamEvent.Usage?.InputTokenCount,
                            CachedTokenCount = streamEvent.Usage?.CachedTokenCount,
                            OutputTokenCount = streamEvent.Usage?.OutputTokenCount,
                            ReasoningTokenCount = streamEvent.Usage?.ReasoningTokenCount,
                            TotalTokenCount = streamEvent.Usage?.TotalTokenCount,
                            RuntimeMs = streamEvent.Usage?.RuntimeMs,
                        }, cancellationToken);
                        break;
                    case "error":
                        await mediator.Send(new LogAgentActivityEvent
                        {
                            Direction = AgentActivityDirection.Response,
                            RunId = runId,
                            CorrelationId = correlationId,
                            Feature = feature,
                            FeatureCategory = featureCategory,
                            SessionId = sessionId ?? string.Empty,
                            Content = responseText.ToString(),
                            ErrorMessage = streamEvent.ErrorMessage,
                        }, cancellationToken);
                        break;
                }

                yield return streamEvent;
            }
        }
        finally
        {
            await enumerator.DisposeAsync();
        }
    }
}
