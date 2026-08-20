using System.Text;
using Core.Chat.Models;
using Core.Chat.Services;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI.Responses;

namespace Core.Chat.Chat2b;

public sealed class Chat2bService : IChatClientService
{
    private readonly IChatSessionStore _sessionStore;
    private readonly ChatAgentSessionStore _agentSessionStore;
    private readonly ChatHostedMcpToolFactory _hostedMcpToolFactory;
    private readonly ChatFoundrySettings _settings;
    private readonly ILogger<Chat2bService> _logger;

    public Chat2bService(
        IChatSessionStore sessionStore,
        ChatAgentSessionStore agentSessionStore,
        ChatHostedMcpToolFactory hostedMcpToolFactory,
        ChatFoundrySettings settings,
        ILogger<Chat2bService> logger)
    {
        _sessionStore = sessionStore;
        _agentSessionStore = agentSessionStore;
        _hostedMcpToolFactory = hostedMcpToolFactory;
        _settings = settings;
        _logger = logger;
    }

    public async IAsyncEnumerable<ChatStreamEvent> SendMessageAsync(
        ChatSendMessageRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var sessionId = ChatResponsesSessionHelper.ResolveSessionId(
            _sessionStore,
            ChatResponsesSessionHelper.Chat2bKind,
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
        var responsesClient = _settings.CreateResponsesClient();

        AIAgent? agent = null;
        AgentSession? agentSession = null;
        string? sessionError = null;
        try
        {
            IList<AITool> mcpTools = _hostedMcpToolFactory.CreateTools();

            agent = responsesClient.AsAIAgent(
                name: "Chat2b",
                instructions: ChatSystemInstructions.WeatherAssistant,
                model: _settings.DeploymentName,
                tools: mcpTools);

            agentSession = await _agentSessionStore.GetOrCreateAsync(agent, sessionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat2b failed to create agent session");
            sessionError = ex.Message;
        }

        if (sessionError is not null)
        {
            yield return ChatStreamEvent.Error(sessionError);
            yield break;
        }

        var assistantBuilder = new StringBuilder();
        var pendingToolCalls = new Dictionary<string, PendingToolCall>();

        IAsyncEnumerable<AgentResponseUpdate>? updates = null;
        string? streamError = null;
        try
        {
            updates = agent!.RunStreamingAsync(userMessage, agentSession, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat2b failed to start streaming");
            streamError = ex.Message;
        }

        if (streamError is not null)
        {
            yield return ChatStreamEvent.Error(streamError);
            yield break;
        }

        await foreach (AgentResponseUpdate update in updates!)
        {
            if (!string.IsNullOrEmpty(update.Text))
            {
                assistantBuilder.Append(update.Text);
                yield return ChatStreamEvent.Token(update.Text);
            }

            foreach (var content in update.Contents)
            {
                usage.Add(content);

                switch (content)
                {
                    case McpServerToolCallContent mcpCall:
                        var toolArguments = ChatToolPayload.Format(mcpCall.Arguments);
                        pendingToolCalls[mcpCall.CallId] = new PendingToolCall(mcpCall.Name, toolArguments);
                        yield return ChatStreamEvent.ToolStart(mcpCall.Name, toolArguments);
                        break;
                    case McpServerToolResultContent mcpResult
                        when pendingToolCalls.Remove(mcpResult.CallId, out var pending):
                        yield return ChatStreamEvent.ToolEnd(
                            pending.Name,
                            pending.Arguments,
                            ChatToolPayload.Format(mcpResult.Outputs));
                        break;
                }
            }
        }

        var assistantText = assistantBuilder.ToString();
        if (!string.IsNullOrWhiteSpace(assistantText))
        {
            _sessionStore.AppendMessage(sessionId, new Models.ChatMessage { Role = "assistant", Content = assistantText });
        }

        yield return ChatStreamEvent.Done(usage.ToChatUsage());
    }

    private sealed record PendingToolCall(string Name, string? Arguments);
}
