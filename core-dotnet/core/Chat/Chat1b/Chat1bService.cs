using System.Text;
using Core.Chat.Models;
using Core.Chat.Services;
using Microsoft.Extensions.Logging;
using OpenAI.Responses;

namespace Core.Chat.Chat1b;

public sealed class Chat1bService : IChatClientService
{
    private readonly IChatSessionStore _sessionStore;
    private readonly ChatMcpToolFactory _mcpToolFactory;
    private readonly ChatFoundrySettings _settings;
    private readonly ILogger<Chat1bService> _logger;

    public Chat1bService(
        IChatSessionStore sessionStore,
        ChatMcpToolFactory mcpToolFactory,
        ChatFoundrySettings settings,
        ILogger<Chat1bService> logger)
    {
        _sessionStore = sessionStore;
        _mcpToolFactory = mcpToolFactory;
        _settings = settings;
        _logger = logger;
    }

    public async IAsyncEnumerable<ChatStreamEvent> SendMessageAsync(
        ChatSendMessageRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var sessionId = ChatResponsesSessionHelper.ResolveSessionId(
            _sessionStore,
            ChatResponsesSessionHelper.Chat1bKind,
            request.SessionId);

        yield return ChatStreamEvent.Session(sessionId);

        var userMessage = request.Message.Trim();
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            yield return ChatStreamEvent.Error("Message cannot be empty.");
            yield break;
        }

        _sessionStore.AppendMessage(sessionId, new Models.ChatMessage { Role = "user", Content = userMessage });

        var history = _sessionStore.GetMessages(sessionId);
        var inputItems = ChatResponsesSessionHelper.BuildInputItems(history.Take(history.Count - 1).ToList(), userMessage);

        var client = _settings.CreateResponsesClient();

        var assistantBuilder = new StringBuilder();

        IAsyncEnumerable<StreamingResponseUpdate>? updates = null;
        string? errorOnStart = null;
        try
        {
            var (latLongTool, weatherTool) = _mcpToolFactory.CreateTools();

            CreateResponseOptions options = new(_settings.DeploymentName, inputItems)
            {
                Tools = { latLongTool, weatherTool },
                StreamingEnabled = true,
            };

            updates = client.CreateResponseStreamingAsync(options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat1b failed to start streaming");
            errorOnStart = ex.Message;
        }

        if (errorOnStart is not null)
        {
            yield return ChatStreamEvent.Error(errorOnStart);
            yield break;
        }

        await foreach (StreamingResponseUpdate update in updates!)
        {
            if (update is StreamingResponseOutputTextDeltaUpdate textDelta && !string.IsNullOrEmpty(textDelta.Delta))
            {
                assistantBuilder.Append(textDelta.Delta);
                yield return ChatStreamEvent.Token(textDelta.Delta);
            }

            // Remote MCP calls are executed by the model host, so the call is only
            // observable once the item completes.
            if (update is StreamingResponseOutputItemDoneUpdate itemDone
                && itemDone.Item is McpToolCallItem mcpCall)
            {
                yield return ChatStreamEvent.ToolStart(mcpCall.ToolName);
                yield return ChatStreamEvent.ToolEnd(mcpCall.ToolName);
            }
        }

        var assistantText = assistantBuilder.ToString();
        if (!string.IsNullOrWhiteSpace(assistantText))
        {
            _sessionStore.AppendMessage(sessionId, new Models.ChatMessage { Role = "assistant", Content = assistantText });
        }

        yield return ChatStreamEvent.Done();
    }
}
