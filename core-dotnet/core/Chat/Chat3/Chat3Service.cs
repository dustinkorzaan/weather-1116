using System.Runtime.ExceptionServices;
using System.Text;
using Azure.AI.Extensions.OpenAI;
using Core.Chat.Models;
using Core.Chat.Services;
using Microsoft.Extensions.Logging;
using OpenAI.Responses;

namespace Core.Chat.Chat3;

/// <summary>
/// Hosted Microsoft Foundry agent (Foundry Console V5 pattern). The app sends
/// only the user prompt; instructions, model, MCP tools, and MCP approval
/// (<c>require_approval: never</c>) are defined on the agent named by
/// <c>AZURE_FOUNDRY_PROD_CHAT_AGENT_NAME</c>. Chat3 does not round-trip
/// tool-call approvals.
/// </summary>
public sealed class Chat3Service : IChatClientService
{
    private readonly IChatSessionStore _sessionStore;
    private readonly ChatHostedAgentResponseStore _responseStore;
    private readonly ChatFoundrySettings _settings;
    private readonly ILogger<Chat3Service> _logger;

    public Chat3Service(
        IChatSessionStore sessionStore,
        ChatHostedAgentResponseStore responseStore,
        ChatFoundrySettings settings,
        ILogger<Chat3Service> logger)
    {
        _sessionStore = sessionStore;
        _responseStore = responseStore;
        _settings = settings;
        _logger = logger;
    }

    public async IAsyncEnumerable<ChatStreamEvent> SendMessageAsync(
        ChatSendMessageRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var sessionId = ChatResponsesSessionHelper.ResolveSessionId(
            _sessionStore,
            ChatResponsesSessionHelper.Chat3Kind,
            request.SessionId);

        yield return ChatStreamEvent.Session(sessionId);

        var userMessage = request.Message?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            yield return ChatStreamEvent.Error("Message cannot be empty.");
            yield break;
        }

        _sessionStore.AppendMessage(sessionId, new Models.ChatMessage { Role = "user", Content = userMessage });

        var usage = new ChatUsageAccumulator();
        ProjectResponsesClient client = null!;
        string conversationId = "";
        ExceptionDispatchInfo? clientFailure = null;
        try
        {
            // Same Foundry client sequence as Console V5. The first turn creates the conversation;
            // later turns reuse it so the agent sees prior messages. If that HTTP call fails, yield an
            // SSE error instead of letting the iterator abort (the UI then shows "network error").
            (client, conversationId) = await _settings.CreateProjectResponsesClientForChatAgentAsync(
                _responseStore.GetConversationId(sessionId),
                cancellationToken);
        }
        catch (Exception ex)
        {
            clientFailure = ExceptionDispatchInfo.Capture(ex);
        }

        if (clientFailure is not null)
        {
            _logger.LogError(
                clientFailure.SourceException,
                "Chat3 failed to create Foundry conversation for agent {AgentName}",
                _settings.ChatAgentName);
            yield return ChatStreamEvent.Error(clientFailure.SourceException.Message);
            yield break;
        }

        var assistantBuilder = new StringBuilder();

        // Conversation state carries prior turns; Foundry rejects previous_response_id alongside it.
        CreateResponseOptions options = new()
        {
            ConversationOptions = new ResponseConversationOptions(),
            AgentConversationId = conversationId,
            StreamingEnabled = true,
            InputItems =
            {
                ResponseItem.CreateUserMessageItem(userMessage),
            },
        };

        IAsyncEnumerator<StreamingResponseUpdate> enumerator = null!;
        ExceptionDispatchInfo? startFailure = null;
        try
        {
            var updates = client.CreateResponseStreamingAsync(options, cancellationToken);
            enumerator = updates.GetAsyncEnumerator(cancellationToken);
        }
        catch (Exception ex)
        {
            startFailure = ExceptionDispatchInfo.Capture(ex);
        }

        if (startFailure is not null)
        {
            _logger.LogError(
                startFailure.SourceException,
                "Chat3 streaming failed to start for agent {AgentName}",
                _settings.ChatAgentName);
            yield return ChatStreamEvent.Error(startFailure.SourceException.Message);
            yield break;
        }

        string? approvalError = null;
        try
        {
            while (true)
            {
                StreamingResponseUpdate? update = null;
                ExceptionDispatchInfo? failure = null;
                try
                {
                    if (await enumerator.MoveNextAsync())
                    {
                        update = enumerator.Current;
                    }
                }
                catch (Exception ex)
                {
                    failure = ExceptionDispatchInfo.Capture(ex);
                }

                if (failure is not null)
                {
                    _logger.LogError(
                        failure.SourceException,
                        "Chat3 streaming failed for agent {AgentName}",
                        _settings.ChatAgentName);
                    yield return ChatStreamEvent.Error(failure.SourceException.Message);
                    yield break;
                }

                if (update is null)
                {
                    break;
                }

                usage.Add(update);

                if (update is StreamingResponseOutputTextDeltaUpdate textDelta && !string.IsNullOrEmpty(textDelta.Delta))
                {
                    assistantBuilder.Append(textDelta.Delta);
                    yield return ChatStreamEvent.Token(textDelta.Delta);
                }

                if (update is not StreamingResponseOutputItemDoneUpdate itemDone)
                {
                    continue;
                }

                switch (itemDone.Item)
                {
                    case McpToolCallItem mcpCall:
                        var toolArguments = ChatToolPayload.Format(mcpCall.ToolArguments);
                        var toolResult = ChatToolPayload.Format(mcpCall.ToolOutput)
                            ?? ChatToolPayload.Format(mcpCall.Error);
                        yield return ChatStreamEvent.ToolStart(mcpCall.ToolName, toolArguments);
                        yield return ChatStreamEvent.ToolEnd(mcpCall.ToolName, toolArguments, toolResult);
                        break;
                    case McpToolCallApprovalRequestItem approvalRequest:
                        approvalError ??=
                            $"Hosted agent '{_settings.ChatAgentName}' requested MCP tool approval " +
                            $"({approvalRequest.ServerLabel}/{approvalRequest.ToolName}). " +
                            "Chat3 sends only the user prompt and does not round-trip approvals. " +
                            "On the agent, set each MCP tool to require_approval: never " +
                            "(Foundry portal: Agents → this agent → Tools → MCP → Approval = Never), then publish a new version.";
                        break;
                }
            }
        }
        finally
        {
            await enumerator.DisposeAsync();
        }

        if (approvalError is not null)
        {
            _logger.LogWarning("{ApprovalError}", approvalError);
            yield return ChatStreamEvent.Error(approvalError);
            yield break;
        }

        // Only a turn that completes cleanly pins the session to this conversation, so a first turn
        // that fails or stops on an MCP approval request leaves the next message a clean conversation.
        _responseStore.SetConversationId(sessionId, conversationId);

        var assistantText = assistantBuilder.ToString();
        if (!string.IsNullOrWhiteSpace(assistantText))
        {
            _sessionStore.AppendMessage(sessionId, new Models.ChatMessage { Role = "assistant", Content = assistantText });
        }

        yield return ChatStreamEvent.Done(usage.ToChatUsage());
    }
}
