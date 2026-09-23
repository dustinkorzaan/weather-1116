using System.Text;
using Core.Chat.Models;
using Core.Chat.Services;
using Core.Chat.Services.ChatScopeGate;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI.Responses;

namespace Core.Chat.Chat5b;

/// <summary>
/// Guardrailed variant of <see cref="Chat4b.Chat4bService"/> (same remote-MCP-tools,
/// multi-agent orchestration shape) with the same five gates as
/// <see cref="Chat5a.Chat5aService"/> — see that class for the gate pipeline/buffered-output
/// design notes, which apply identically here. Chat4bService itself is untouched.
/// </summary>
public sealed class Chat5bService : IChat5ClientService
{
    private readonly IChatSessionStore _sessionStore;
    private readonly ChatAgentSessionStore _agentSessionStore;
    private readonly ChatHostedMcpToolFactory _hostedMcpToolFactory;
    private readonly ChatFoundrySettings _settings;
    private readonly MaxLengthScopeGate _maxLengthGate;
    private readonly RuleScopeGate _ruleInputGate;
    private readonly IScopeGate _llmInputGate;
    private readonly IScopeGate _llmOutputGate;
    private readonly ILogger<Chat5bService> _logger;

    public Chat5bService(
        IChatSessionStore sessionStore,
        ChatAgentSessionStore agentSessionStore,
        ChatHostedMcpToolFactory hostedMcpToolFactory,
        ChatFoundrySettings settings,
        MaxLengthScopeGate maxLengthGate,
        RuleScopeGate ruleInputGate,
        [FromKeyedServices("Chat5InputLlmGate")] IScopeGate llmInputGate,
        [FromKeyedServices("Chat5OutputLlmGate")] IScopeGate llmOutputGate,
        ILogger<Chat5bService> logger)
    {
        _sessionStore = sessionStore;
        _agentSessionStore = agentSessionStore;
        _hostedMcpToolFactory = hostedMcpToolFactory;
        _settings = settings;
        _maxLengthGate = maxLengthGate;
        _ruleInputGate = ruleInputGate;
        _llmInputGate = llmInputGate;
        _llmOutputGate = llmOutputGate;
        _logger = logger;
    }

    public async IAsyncEnumerable<ChatStreamEvent> SendMessageAsync(
        Chat5SendMessageRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var sessionId = ChatResponsesSessionHelper.ResolveSessionId(
            _sessionStore,
            ChatResponsesSessionHelper.Chat5bKind,
            request.SessionId);

        yield return ChatStreamEvent.Session(sessionId);

        var userMessage = request.Message?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            yield return ChatStreamEvent.Error("Message cannot be empty.");
            yield break;
        }

        string? blockedReason = null;
        string? gateError = null;
        try
        {
            blockedReason = await RunInputGatesAsync(request, userMessage, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat5b input gate failed");
            gateError = ex.Message;
        }

        if (gateError is not null)
        {
            yield return ChatStreamEvent.Error(gateError);
            yield break;
        }

        // Recorded in this UI-visible chat history whether the request proceeds or a gate
        // blocks it below, so the transcript reflects what was actually typed either way. This
        // is display history only, not the orchestrator's own memory: _sessionStore is never
        // read back by the model, and a blocked turn never calls RunStreamingAsync/RunAsync, so
        // the orchestrator itself never sees a blocked prompt.
        _sessionStore.AppendMessage(sessionId, new Models.ChatMessage { Role = "user", Content = userMessage });

        if (blockedReason is not null)
        {
            yield return ChatStreamEvent.Blocked(blockedReason);
            yield return ChatStreamEvent.Done(new ChatUsageAccumulator().ToChatUsage());
            yield break;
        }

        var usage = new ChatUsageAccumulator();
        var responsesClient = _settings.CreateResponsesClient();

        AIAgent? orchestrationAgent = null;
        AgentSession? agentSession = null;
        string? sessionError = null;
        try
        {
            // BuildOrchestrationAgent calls ChatHostedMcpToolFactory, which throws when
            // MCP_SRV_* env vars are missing — build it inside this try (like Chat4b does) so
            // missing MCP config surfaces as a ChatStreamEvent.Error, not an unhandled exception.
            orchestrationAgent = BuildOrchestrationAgent(responsesClient, request.EnableSystemPromptGuard);
            agentSession = await _agentSessionStore.GetOrCreateAsync(orchestrationAgent, sessionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat5b failed to create agent session");
            sessionError = ex.Message;
        }

        if (sessionError is not null)
        {
            yield return ChatStreamEvent.Error(sessionError);
            yield break;
        }

        if (request.EnableLlmOutputGate)
        {
            await foreach (var evt in RunBufferedAsync(orchestrationAgent!, agentSession!, userMessage, sessionId, usage, cancellationToken))
            {
                yield return evt;
            }
            yield break;
        }

        var assistantBuilder = new StringBuilder();
        var pendingToolCalls = new Dictionary<string, PendingToolCall>();

        IAsyncEnumerable<AgentResponseUpdate>? updates = null;
        string? streamError = null;
        try
        {
            updates = orchestrationAgent!.RunStreamingAsync(userMessage, agentSession, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat5b failed to start streaming");
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
                    case FunctionCallContent functionCall:
                        var toolArguments = ChatToolPayload.Format(functionCall.Arguments);
                        pendingToolCalls[functionCall.CallId] = new PendingToolCall(functionCall.Name, toolArguments);
                        yield return ChatStreamEvent.ToolStart(functionCall.Name, toolArguments);
                        break;
                    case FunctionResultContent functionResult
                        when pendingToolCalls.Remove(functionResult.CallId, out var pending):
                        yield return ChatStreamEvent.ToolEnd(
                            pending.Name,
                            pending.Arguments,
                            ChatToolPayload.Format(functionResult.Result));
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

    /// <summary>
    /// Gate #5 ("LLM Output") path — see Chat5aService.RunBufferedAsync for design notes;
    /// identical mechanics here, just against the remote-MCP-tools orchestrator.
    /// </summary>
    private async IAsyncEnumerable<ChatStreamEvent> RunBufferedAsync(
        AIAgent orchestrationAgent,
        AgentSession agentSession,
        string userMessage,
        string sessionId,
        ChatUsageAccumulator usage,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        AgentResponse? response = null;
        string? runError = null;
        try
        {
            response = await orchestrationAgent.RunAsync(userMessage, agentSession, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat5b failed to run buffered turn");
            runError = ex.Message;
        }

        if (runError is not null)
        {
            yield return ChatStreamEvent.Error(runError);
            yield break;
        }

        usage.Add(response!.Usage);

        var pendingToolCalls = new Dictionary<string, PendingToolCall>();
        foreach (var message in response.Messages)
        {
            foreach (var content in message.Contents)
            {
                switch (content)
                {
                    case FunctionCallContent functionCall:
                        var toolArguments = ChatToolPayload.Format(functionCall.Arguments);
                        pendingToolCalls[functionCall.CallId] = new PendingToolCall(functionCall.Name, toolArguments);
                        yield return ChatStreamEvent.ToolStart(functionCall.Name, toolArguments);
                        break;
                    case FunctionResultContent functionResult
                        when pendingToolCalls.Remove(functionResult.CallId, out var pending):
                        yield return ChatStreamEvent.ToolEnd(
                            pending.Name,
                            pending.Arguments,
                            ChatToolPayload.Format(functionResult.Result));
                        break;
                }
            }
        }

        var assistantText = response.Text ?? string.Empty;

        ChatScopeGateResult? outputVerdict = null;
        string? outputGateError = null;
        try
        {
            outputVerdict = await _llmOutputGate.EvaluateAsync(assistantText, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat5b output gate failed");
            outputGateError = ex.Message;
        }

        if (outputGateError is not null)
        {
            yield return ChatStreamEvent.Error(outputGateError);
            yield break;
        }

        if (!outputVerdict!.InScope)
        {
            yield return ChatStreamEvent.Blocked(
                $"Blocked by {_llmOutputGate.Name}: {outputVerdict.Reason ?? "response is out of scope"}");
            yield return ChatStreamEvent.Done(usage.ToChatUsage());
            yield break;
        }

        if (!string.IsNullOrWhiteSpace(assistantText))
        {
            _sessionStore.AppendMessage(sessionId, new Models.ChatMessage { Role = "assistant", Content = assistantText });
            yield return ChatStreamEvent.Token(assistantText);
        }

        yield return ChatStreamEvent.Done(usage.ToChatUsage());
    }

    /// <summary>
    /// Gates #1/#2/#3 — see Chat5aService.RunInputGatesAsync for design notes; identical here.
    /// </summary>
    private Task<string?> RunInputGatesAsync(Chat5SendMessageRequest request, string userMessage, CancellationToken cancellationToken)
    {
        var gates = new List<IScopeGate>();
        if (request.EnableMaxLengthGate) gates.Add(_maxLengthGate);
        if (request.EnableRuleInputGate) gates.Add(_ruleInputGate);
        if (request.EnableLlmInputGate) gates.Add(_llmInputGate);

        return ChatScopeGatePipeline.RunAsync(gates, userMessage, cancellationToken);
    }

    private sealed record PendingToolCall(string Name, string? Arguments);

    private AIAgent BuildOrchestrationAgent(ResponsesClient responsesClient, bool useHardenedPrompt)
    {
        // Agent Geo 👤: geo sub-agent — location name ↔ latitude/longitude and nearby cities, via the
        // mcp-srv-func-app (GetLatLong/GetLocation) and mcp-srv-python (GetCities) remote MCP hosts.
        AIAgent geoAgent = responsesClient.AsAIAgent(
            name: "Geo",
            instructions: ChatSystemInstructions.MultiAgentGeoAssistant,
            model: _settings.DeploymentName,
            tools: _hostedMcpToolFactory.CreateGeoTools());

        // Agent NonAI Weather 👤: weather sub-agent — current/forecast/history for a given
        // lat/long only, via the mcp-srv-node (current/forecast/history) remote MCP host.
        AIAgent nonAiWeatherAgent = responsesClient.AsAIAgent(
            name: "NonAIWeather",
            instructions: ChatSystemInstructions.MultiAgentNonAiWeatherAssistant,
            model: _settings.DeploymentName,
            tools: _hostedMcpToolFactory.CreateNonAiWeatherTools());

        // Agent User 👤: user sub-agent — lists, adds, and deletes the user's saved map pins, via the
        // mcp-srv-app-service (GetUser/AddUserPin/DeleteUserPin) remote MCP host.
        AIAgent userAgent = responsesClient.AsAIAgent(
            name: "User",
            instructions: ChatSystemInstructions.MultiAgentUserAssistant,
            model: _settings.DeploymentName,
            tools: _hostedMcpToolFactory.CreateUserTools());

        // Agent AI Weather Orchestration 👤: orchestrator — delegates to Geo, NonAI Weather, and User, holds the multi-turn session.
        // Gate #4 ("Sys Prompt"): when checked, uses the hardened instructions with the
        // refusal paragraph; when unchecked, the same plain instructions Chat4b uses.
        return responsesClient.AsAIAgent(
            name: "AIWeatherOrchestration",
            instructions: useHardenedPrompt
                ? ChatSystemInstructions.Chat5HardenedAiWeatherOrchestrationAssistant
                : ChatSystemInstructions.MultiAgentAiWeatherOrchestrationAssistant,
            model: _settings.DeploymentName,
            tools:
            [
                // session omitted — AsAIFunction creates a fresh, throwaway session per call, so Geo is
                // stateless per delegated call; the orchestrator alone owns memory.
                geoAgent.AsAIFunction(new AIFunctionFactoryOptions
                {
                    Name = "Geo",
                    Description = "Geo assistant. Resolves a location name to latitude/longitude, or reverse-geocodes latitude/longitude to a place label. Send it a natural-language geo question; it returns the answer as text.",
                }),
                // session omitted — AsAIFunction creates a fresh, throwaway session per call, so NonAI
                // Weather is stateless per delegated call; the orchestrator alone owns memory.
                nonAiWeatherAgent.AsAIFunction(new AIFunctionFactoryOptions
                {
                    Name = "NonAIWeather",
                    Description = "Weather assistant. Reports current conditions, an upcoming forecast (daily, hourly, or every 15 minutes), or recent history (daily or hourly) for a latitude/longitude. Accepts numeric coordinates only — resolve a place name to coordinates via Geo first. It has no memory of its own, so include the coordinates on every call, including follow-up turns. Send it a natural-language weather question that names the coordinates and the level of detail you want (daily, hourly, or every 15 minutes); it returns the answer as text.",
                }),
                // session omitted — AsAIFunction creates a fresh, throwaway session per call, so User
                // is stateless per delegated call; the orchestrator alone owns memory.
                userAgent.AsAIFunction(new AIFunctionFactoryOptions
                {
                    Name = "User",
                    Description = "User assistant. Lists the user's saved map pins (location name, latitude/longitude, and id), adds a pin from numeric latitude/longitude and a location name, or deletes a pin by its id. It never geocodes — resolve a place name to coordinates via Geo first. It has no memory of its own, so include the pin id or coordinates on every call. Send it a natural-language request; it returns the answer as text.",
                }),
            ]);
    }
}
