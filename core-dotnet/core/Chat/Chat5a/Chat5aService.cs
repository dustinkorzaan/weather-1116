using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Core.Chat.Models;
using Core.Chat.Services;
using Core.Chat.Services.ChatScopeGate;
using Core.Geo.Events;
using Core.Json;
using Core.Weather.Events;
using CQMediator;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI.Responses;

namespace Core.Chat.Chat5a;

/// <summary>
/// Guardrailed variant of <see cref="Chat4a.Chat4aService"/> (same in-process-tools,
/// multi-agent orchestration shape) with five independently toggleable guardrail gates:
/// 500 Char / Code Input / LLM Input (pre-flight, run in <see cref="RunInputGatesAsync"/>),
/// Sys Prompt (swaps the orchestrator's system instructions), and LLM Output (post-flight,
/// forces a non-streamed/buffered turn so the full reply can be classified before release).
/// Chat4aService itself is untouched — this is a full independent copy, not a wrapper, so it
/// stays comparable as the "unsecured" baseline.
/// </summary>
public sealed class Chat5aService : IChat5ClientService
{
    private readonly IChatSessionStore _sessionStore;
    private readonly ChatAgentSessionStore _agentSessionStore;
    private readonly IMediator _mediator;
    private readonly ChatFoundrySettings _settings;
    private readonly MaxLengthScopeGate _maxLengthGate;
    private readonly RuleScopeGate _ruleInputGate;
    private readonly IScopeGate _llmInputGate;
    private readonly IScopeGate _llmOutputGate;
    private readonly ILogger<Chat5aService> _logger;

    public Chat5aService(
        IChatSessionStore sessionStore,
        ChatAgentSessionStore agentSessionStore,
        IMediator mediator,
        ChatFoundrySettings settings,
        MaxLengthScopeGate maxLengthGate,
        RuleScopeGate ruleInputGate,
        [FromKeyedServices("Chat5InputLlmGate")] IScopeGate llmInputGate,
        [FromKeyedServices("Chat5OutputLlmGate")] IScopeGate llmOutputGate,
        ILogger<Chat5aService> logger)
    {
        _sessionStore = sessionStore;
        _agentSessionStore = agentSessionStore;
        _mediator = mediator;
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
            ChatResponsesSessionHelper.Chat5aKind,
            request.SessionId);

        yield return ChatStreamEvent.Session(sessionId);

        var userMessage = request.Message?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            yield return ChatStreamEvent.Error("Message cannot be empty.");
            yield break;
        }

        // Kept in history even if a gate below blocks it, so follow-up turns retain full
        // context of what was actually typed.
        _sessionStore.AppendMessage(sessionId, new Models.ChatMessage { Role = "user", Content = userMessage });

        string? blockedReason = null;
        string? gateError = null;
        try
        {
            blockedReason = await RunInputGatesAsync(request, userMessage, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat5a input gate failed");
            gateError = ex.Message;
        }

        if (gateError is not null)
        {
            yield return ChatStreamEvent.Error(gateError);
            yield break;
        }

        if (blockedReason is not null)
        {
            yield return ChatStreamEvent.Blocked(blockedReason);
            yield return ChatStreamEvent.Done(new ChatUsageAccumulator().ToChatUsage());
            yield break;
        }

        var usage = new ChatUsageAccumulator();
        var responsesClient = _settings.CreateResponsesClient();
        AIAgent orchestrationAgent = BuildOrchestrationAgent(responsesClient, request.EnableSystemPromptGuard);

        AgentSession? agentSession = null;
        string? sessionError = null;
        try
        {
            agentSession = await _agentSessionStore.GetOrCreateAsync(orchestrationAgent, sessionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat5a failed to create agent session");
            sessionError = ex.Message;
        }

        if (sessionError is not null)
        {
            yield return ChatStreamEvent.Error(sessionError);
            yield break;
        }

        if (request.EnableLlmOutputGate)
        {
            await foreach (var evt in RunBufferedAsync(orchestrationAgent, agentSession!, userMessage, sessionId, usage, cancellationToken))
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
            updates = orchestrationAgent.RunStreamingAsync(userMessage, agentSession, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat5a failed to start streaming");
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
    /// Gate #5 ("LLM Output") path: no <see cref="AIAgent.RunStreamingAsync"/> here, since the
    /// full reply has to exist before it can be classified. Tool delegation is replayed from
    /// <see cref="AgentResponse.Messages"/> as tool_start/tool_end pairs so it's still visible,
    /// then the reply streams to the client as a single Token event (or is Blocked).
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
            _logger.LogError(ex, "Chat5a failed to run buffered turn");
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
            _logger.LogError(ex, "Chat5a output gate failed");
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
    /// Gates #1/#2/#3 ("500 Char" / "Code Input" / "LLM Input"), cheapest-first with AND
    /// semantics: the first enabled gate to fail blocks the request and the orchestrator never
    /// runs. Returns the formatted Blocked message, or null if every enabled gate passed.
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
        // Agent Geo 👤: geo sub-agent — resolves location name ↔ latitude/longitude only.
        AIAgent geoAgent = responsesClient.AsAIAgent(
            name: "Geo",
            instructions: ChatSystemInstructions.MultiAgentGeoAssistant,
            model: _settings.DeploymentName,
            tools: CreateGeoTools());

        // Agent NonAI Weather 👤: weather sub-agent — current/forecast/history for a given lat/long only.
        AIAgent nonAiWeatherAgent = responsesClient.AsAIAgent(
            name: "NonAIWeather",
            instructions: ChatSystemInstructions.MultiAgentNonAiWeatherAssistant,
            model: _settings.DeploymentName,
            tools: CreateNonAiWeatherTools());

        // Agent AI Weather Orchestration 👤: orchestrator — delegates to Geo and NonAI Weather, holds the multi-turn session.
        // Gate #4 ("Sys Prompt"): when checked, uses the hardened instructions with the
        // refusal paragraph; when unchecked, the same plain instructions Chat4a uses.
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
            ]);
    }

    // Agent Geo 👤's tools: geo resolution only.
    private IList<AITool> CreateGeoTools() =>
    [
        AIFunctionFactory.Create(GetLatLong),
        AIFunctionFactory.Create(GetLocation),
    ];

    // Agent NonAI Weather 👤's tools: weather facts only.
    private IList<AITool> CreateNonAiWeatherTools() =>
    [
        AIFunctionFactory.Create(GetPublicWeatherCurrent),
        AIFunctionFactory.Create(GetPublicWeatherForecast),
        AIFunctionFactory.Create(GetPublicWeatherHistory),
    ];

    [Description("Resolve a location name to ranked latitude/longitude matches using public geocoding data. Returns up to 5 results (rank 1 is the best match). Use state and country to pick the right place if rank 1 is wrong.")]
    private async Task<string> GetLatLong(
        [Description("City and optional region/country, e.g. Nashville, TN")] string location,
        CancellationToken cancellationToken)
    {
        var latLongMatches = await _mediator.Send(new GetLatLongEvent { Location = location }, cancellationToken);
        return JsonSerializer.Serialize(latLongMatches, JsonDefaults.Pretty);
    }

    [Description("Turn a latitude and longitude into a simple place label. Prefers City, State in the US (City, State, Country elsewhere), then a feature name, then a formatted coordinate such as 35.51° N, 86.58° W.")]
    private async Task<string> GetLocation(
        [Description("Latitude in decimal degrees")] double latitude,
        [Description("Longitude in decimal degrees")] double longitude,
        CancellationToken cancellationToken)
    {
        var locationData = await _mediator.Send(new GetLocationEvent
        {
            Latitude = latitude,
            Longitude = longitude,
        }, cancellationToken);
        return JsonSerializer.Serialize(locationData, JsonDefaults.Pretty);
    }

    [Description("Get current public weather conditions for a latitude and longitude.")]
    private async Task<string> GetPublicWeatherCurrent(
        [Description("Latitude in decimal degrees")] double latitude,
        [Description("Longitude in decimal degrees")] double longitude,
        CancellationToken cancellationToken)
    {
        var weatherData = await _mediator.Send(new GetPublicWeatherCurrentEvent
        {
            Latitude = latitude,
            Longitude = longitude,
        }, cancellationToken);

        return JsonSerializer.Serialize(weatherData, JsonDefaults.Pretty);
    }

    [Description("Get an upcoming public weather forecast for a latitude and longitude. Daily is the next 7 days, Hourly is the next 48 hours, and FifteenMinutes is the next 48 hours in 15-minute steps. Use Daily unless the user asks for hourly or 15-minute detail.")]
    private async Task<string> GetPublicWeatherForecast(
        [Description("Latitude in decimal degrees")] double latitude,
        [Description("Longitude in decimal degrees")] double longitude,
        [Description("Daily (next 7 days), Hourly (next 48 hours), or FifteenMinutes (next 48 hours). Defaults to Daily.")]
        PublicWeatherForecastResolution resolution,
        CancellationToken cancellationToken)
    {
        var weatherData = await _mediator.Send(new GetPublicWeatherForecastEvent
        {
            Latitude = latitude,
            Longitude = longitude,
            Resolution = resolution,
        }, cancellationToken);

        return JsonSerializer.Serialize(weatherData, JsonDefaults.Pretty);
    }

    [Description("Get recent past public weather for a latitude and longitude. Daily is the previous 7 days, Hourly is the previous 48 hours. Use Daily unless the user asks for hourly detail.")]
    private async Task<string> GetPublicWeatherHistory(
        [Description("Latitude in decimal degrees")] double latitude,
        [Description("Longitude in decimal degrees")] double longitude,
        [Description("Daily (previous 7 days) or Hourly (previous 48 hours). Defaults to Daily.")]
        PublicWeatherHistoryResolution resolution,
        CancellationToken cancellationToken)
    {
        var weatherData = await _mediator.Send(new GetPublicWeatherHistoryEvent
        {
            Latitude = latitude,
            Longitude = longitude,
            Resolution = resolution,
        }, cancellationToken);

        return JsonSerializer.Serialize(weatherData, JsonDefaults.Pretty);
    }
}
