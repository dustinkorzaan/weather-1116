using System.Runtime.CompilerServices;
using System.Text;
using Core.Chat.Models;
using Core.Data;

namespace Core.Chat.Services;

/// <summary>
/// Decorates a Chat1a-Chat4b <see cref="IChatClientService"/> with dbo.AgentActivity logging.
/// Registered once per keyed service in <see cref="ChatServiceCollectionExtensions"/> so none of
/// the 7 chat tabs, their controllers, or their Core services need to know this exists.
/// </summary>
public class AgentActivityLoggingChatClientService(
    IChatClientService inner,
    IAgentActivityLogger activityLogger,
    string feature,
    string featureCategory) : IChatClientService
{
    public async IAsyncEnumerable<ChatStreamEvent> SendMessageAsync(
        ChatSendMessageRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // request.SessionId is null on a brand-new chat; the "session" event below carries the
        // id the underlying service assigns, so the Response row always has the real one even
        // when the Request row was logged before that id existed.
        var correlationId = await activityLogger.LogRequestAsync(
            feature,
            featureCategory,
            sessionId: request.SessionId ?? string.Empty,
            content: request.Message,
            cancellationToken: cancellationToken);

        var sessionId = request.SessionId;
        var responseText = new StringBuilder();

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
                case "done":
                    await activityLogger.LogResponseAsync(
                        correlationId,
                        feature,
                        featureCategory,
                        sessionId: sessionId ?? string.Empty,
                        content: responseText.ToString(),
                        inputTokenCount: streamEvent.Usage?.InputTokenCount,
                        cachedTokenCount: streamEvent.Usage?.CachedTokenCount,
                        outputTokenCount: streamEvent.Usage?.OutputTokenCount,
                        reasoningTokenCount: streamEvent.Usage?.ReasoningTokenCount,
                        totalTokenCount: streamEvent.Usage?.TotalTokenCount,
                        runtimeMs: streamEvent.Usage?.RuntimeMs,
                        cancellationToken: cancellationToken);
                    break;
                case "error":
                    await activityLogger.LogResponseAsync(
                        correlationId,
                        feature,
                        featureCategory,
                        sessionId: sessionId ?? string.Empty,
                        content: responseText.ToString(),
                        errorMessage: streamEvent.ErrorMessage,
                        cancellationToken: cancellationToken);
                    break;
            }

            yield return streamEvent;
        }
    }
}
