using System.Text;
using Core.Chat.Models;
using Core.Chat.Services;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI.Responses;

namespace Core.Chat.Chat4b;

/// <summary>
/// Microsoft Agent Framework, remote MCP tools, multi-agent (V4 orchestration style).
/// Same four-agent shape as Chat4a: Agent AI Weather Orchestration 👤 is the orchestrator the
/// user talks to and delegates to Agent Geo 👤 (geo), Agent NonAI Weather 👤 (weather), and Agent
/// User 👤 (saved map pins), each wrapped as a callable tool via
/// <see cref="AIAgentExtensions.AsAIFunction"/>. Only the orchestrator carries a persistent
/// <see cref="AgentSession"/> (multi-turn memory); the sub-agents are rebuilt per request.
/// Omitting <c>session</c> from AsAIFunction does not leave it null — a fresh, throwaway
/// <see cref="AgentSession"/> is created for each delegated call, which is what makes the
/// sub-agents stateless, same as Chat4a.
/// The difference from Chat4a: the sub-agents get their tools from the existing remote MCP
/// hosts (<see cref="ChatHostedMcpToolFactory"/>) instead of in-process CQMediator calls — Geo
/// gets the <c>mcp-srv-func-app</c> (GetLatLong/GetLocation) and <c>mcp-srv-python</c> (GetCities)
/// tools, NonAI Weather gets the <c>mcp-srv-node</c> (current/forecast/history) tools, and User
/// gets the <c>mcp-srv-app-service</c> (GetUser/AddUserPin/DeleteUserPin) tools. From the
/// orchestrator's point of view nothing changes: the sub-agents are still ordinary
/// <c>AsAIFunction</c>-wrapped tools, so the orchestrator's
/// stream still shows <see cref="FunctionCallContent"/>/<see cref="FunctionResultContent"/>, not
/// MCP content types — those only ever appear inside each sub-agent's own non-streamed
/// <c>AsAIFunction</c>-generated call, invisible to the outer SSE stream, the same nested-call
/// blind spot Chat4a has (including the same usage-chip undercount for the same reason).
/// </summary>
public sealed class Chat4bService : IChatClientService
{
    private readonly IChatSessionStore _sessionStore;
    private readonly ChatAgentSessionStore _agentSessionStore;
    private readonly ChatHostedMcpToolFactory _hostedMcpToolFactory;
    private readonly ChatFoundrySettings _settings;
    private readonly ILogger<Chat4bService> _logger;

    public Chat4bService(
        IChatSessionStore sessionStore,
        ChatAgentSessionStore agentSessionStore,
        ChatHostedMcpToolFactory hostedMcpToolFactory,
        ChatFoundrySettings settings,
        ILogger<Chat4bService> logger)
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
            ChatResponsesSessionHelper.Chat4bKind,
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

        AIAgent? orchestrationAgent = null;
        AgentSession? agentSession = null;
        string? sessionError = null;
        try
        {
            // BuildOrchestrationAgent calls ChatHostedMcpToolFactory, which throws when
            // MCP_SRV_* env vars are missing — build it inside this try (like Chat2b does with
            // CreateTools()) so missing MCP config surfaces as a ChatStreamEvent.Error, not an
            // unhandled exception.
            orchestrationAgent = BuildOrchestrationAgent(responsesClient);
            agentSession = await _agentSessionStore.GetOrCreateAsync(orchestrationAgent, sessionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat4b failed to create agent session");
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
            updates = orchestrationAgent!.RunStreamingAsync(userMessage, agentSession, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat4b failed to start streaming");
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

    private sealed record PendingToolCall(string Name, string? Arguments);

    private AIAgent BuildOrchestrationAgent(ResponsesClient responsesClient)
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
        return responsesClient.AsAIAgent(
            name: "AIWeatherOrchestration",
            instructions: ChatSystemInstructions.MultiAgentAiWeatherOrchestrationAssistant,
            model: _settings.DeploymentName,
            tools:
            [
                // session omitted — AsAIFunction creates a fresh, throwaway session per call, so Geo is
                // stateless per delegated call; the orchestrator alone owns memory.
                geoAgent.AsAIFunction(new AIFunctionFactoryOptions
                {
                    Name = "Geo",
                    Description = "Geo assistant. Resolves a location name to latitude/longitude, reverse-geocodes latitude/longitude to a place label, or lists the largest cities within a radius of a latitude/longitude. Send it a natural-language geo question; it returns the answer as text.",
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
