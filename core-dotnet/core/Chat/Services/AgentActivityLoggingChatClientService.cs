using System.Runtime.CompilerServices;
using System.Text;
using Core.Chat.Models;
using Core.Data.Domain;
using Core.Data.Events;
using CQMediator;

namespace Core.Chat.Services;

/// <summary>
/// Decorates a Chat1a-Chat4b <see cref="IChatClientService"/> with dbo.AgentActivity logging.
/// Registered once per keyed service in <see cref="ChatServiceCollectionExtensions"/> so none of
/// the 7 chat tabs, their controllers, or their Core services need to know this exists.
///
/// Every tab's own tool_start/tool_end SSE events (already flowing through this decorator) are
/// also logged as nested Request/Response rows sharing the turn's TraceId. For Chat4a/Chat4b,
/// those events are the orchestrator's delegation calls to its named sub-agents ("Geo",
/// "NonAIWeather" -- see docs/5-chat-clients/5-chat-clients.md), so featureCategory decides
/// whether the tool name is logged as AgentName (MultiAgent) or ToolName (everything else). Geo's
/// and NonAI Weather's own inner tool calls stay invisible here -- same nested blind spot the SSE
/// stream and usage chip already have (documented in 5-chat-clients.md).
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
        var traceId = Guid.NewGuid();

        // request.SessionId is null on a brand-new chat; the "session" event below carries the
        // id the underlying service assigns, so later rows always have the real one even when
        // this Request row was logged before that id existed.
        var correlationId = await mediator.Send(new LogAgentActivityEvent
        {
            Direction = AgentActivityDirection.Request,
            TraceId = traceId,
            Feature = feature,
            FeatureCategory = featureCategory,
            SessionId = request.SessionId ?? string.Empty,
            Content = request.Message,
        }, cancellationToken);

        var sessionId = request.SessionId;
        var responseText = new StringBuilder();
        var loopNumber = 0;
        var pendingToolCallIds = new Dictionary<string, Queue<Guid>>();

        await foreach (var streamEvent in inner.SendMessageAsync(request, cancellationToken))
        {
            switch (streamEvent.Type)
            {
                case "session":
                    sessionId = streamEvent.SessionId ?? sessionId;
                    break;
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
                            TraceId = traceId,
                            Feature = feature,
                            FeatureCategory = featureCategory,
                            SessionId = sessionId ?? string.Empty,
                            AgentName = featureCategory == AgentActivityFeatureCategory.MultiAgent ? streamEvent.ToolName : null,
                            ToolName = featureCategory == AgentActivityFeatureCategory.MultiAgent ? null : streamEvent.ToolName,
                            LoopNumber = loopNumber,
                            Content = streamEvent.ToolArguments,
                        }, cancellationToken);

                        if (!pendingToolCallIds.TryGetValue(streamEvent.ToolName, out var queue))
                        {
                            queue = new Queue<Guid>();
                            pendingToolCallIds[streamEvent.ToolName] = queue;
                        }

                        queue.Enqueue(toolCallId);
                    }
                    break;
                case "tool_end":
                    if (streamEvent.ToolName is not null
                        && pendingToolCallIds.TryGetValue(streamEvent.ToolName, out var pendingQueue)
                        && pendingQueue.TryDequeue(out var pendingCorrelationId))
                    {
                        await mediator.Send(new LogAgentActivityEvent
                        {
                            Direction = AgentActivityDirection.Response,
                            TraceId = traceId,
                            CorrelationId = pendingCorrelationId,
                            Feature = feature,
                            FeatureCategory = featureCategory,
                            SessionId = sessionId ?? string.Empty,
                            AgentName = featureCategory == AgentActivityFeatureCategory.MultiAgent ? streamEvent.ToolName : null,
                            ToolName = featureCategory == AgentActivityFeatureCategory.MultiAgent ? null : streamEvent.ToolName,
                            Content = streamEvent.ToolResult,
                        }, cancellationToken);
                    }
                    break;
                case "done":
                    await mediator.Send(new LogAgentActivityEvent
                    {
                        Direction = AgentActivityDirection.Response,
                        TraceId = traceId,
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
                        TraceId = traceId,
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
}
