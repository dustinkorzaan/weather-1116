using System.Text;
using Core.Chat.Models;
using Core.Chat.Services;
using Microsoft.Extensions.Logging;
using OpenAI.Responses;

namespace Core.Chat.Chat3;

/// <summary>
/// Hosted Microsoft Foundry agent (Foundry Console V5 pattern). The app sends
/// only the user prompt; instructions, model, and MCP tools are defined on
/// the agent named by <c>AZURE_FOUNDRY_PROD_EUS2_CHAT_AGENT_NAME</c>.
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

        var client = _settings.CreateProjectResponsesClientForChatAgent();
        var assistantBuilder = new StringBuilder();
        var pendingApprovals = new List<McpToolCallApprovalRequestItem>();
        var previousResponseId = _responseStore.GetPreviousResponseId(sessionId);
        var sendUserMessage = true;

        while (true)
        {
            CreateResponseOptions options = new()
            {
                StreamingEnabled = true,
                StoredOutputEnabled = true,
            };

            if (!string.IsNullOrWhiteSpace(previousResponseId))
            {
                options.PreviousResponseId = previousResponseId;
            }

            if (sendUserMessage)
            {
                options.InputItems.Add(ResponseItem.CreateUserMessageItem(userMessage));
                sendUserMessage = false;
            }
            else
            {
                foreach (var approvalRequest in pendingApprovals)
                {
                    options.InputItems.Add(ResponseItem.CreateMcpApprovalResponseItem(
                        approvalRequestId: approvalRequest.Id,
                        approved: true));
                }

                pendingApprovals.Clear();
            }

            IAsyncEnumerable<StreamingResponseUpdate>? updates = null;
            string? errorOnStart = null;
            try
            {
                updates = client.CreateResponseStreamingAsync(options, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chat3 failed to start streaming for agent {AgentName}", _settings.ChatAgentName);
                errorOnStart = ex.Message;
            }

            if (errorOnStart is not null)
            {
                yield return ChatStreamEvent.Error(errorOnStart);
                yield break;
            }

            await foreach (StreamingResponseUpdate update in updates!)
            {
                if (update is StreamingResponseCreatedUpdate created
                    && !string.IsNullOrWhiteSpace(created.Response?.Id))
                {
                    previousResponseId = created.Response.Id;
                }

                if (update is StreamingResponseCompletedUpdate completed
                    && !string.IsNullOrWhiteSpace(completed.Response?.Id))
                {
                    previousResponseId = completed.Response.Id;
                }

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
                        _logger.LogInformation(
                            "Chat3 auto-approving MCP tool {ToolName} on {ServerLabel}",
                            approvalRequest.ToolName,
                            approvalRequest.ServerLabel);
                        pendingApprovals.Add(approvalRequest);
                        break;
                }
            }

            if (pendingApprovals.Count == 0)
            {
                break;
            }
        }

        if (!string.IsNullOrWhiteSpace(previousResponseId))
        {
            _responseStore.SetPreviousResponseId(sessionId, previousResponseId);
        }

        var assistantText = assistantBuilder.ToString();
        if (!string.IsNullOrWhiteSpace(assistantText))
        {
            _sessionStore.AppendMessage(sessionId, new Models.ChatMessage { Role = "assistant", Content = assistantText });
        }

        yield return ChatStreamEvent.Done();
    }
}
