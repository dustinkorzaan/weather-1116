using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Core.Chat.Models;
using Core.Chat.Services;
using Core.Geo.Events;
using Core.Json;
using Core.Weather.Events;
using CQMediator;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI.Responses;

namespace Core.Chat.Chat4a;

/// <summary>
/// Microsoft Agent Framework, in-process tools, multi-agent (V3 orchestration style).
/// Three agents: Agent AI Weather Orchestration 👤 is the orchestrator the user talks to and
/// delegates to Agent Geo 👤 (geo) and Agent NonAI Weather 👤 (weather), each wrapped as a
/// callable tool via <see cref="AIAgentExtensions.AsAIFunction"/>. Only the orchestrator carries
/// a persistent <see cref="AgentSession"/> (multi-turn memory); Geo and NonAI Weather are
/// rebuilt per request. Omitting <c>session</c> from AsAIFunction does not leave it null — a
/// fresh, throwaway <see cref="AgentSession"/> is created for each delegated call, which is what
/// makes Geo and NonAI Weather stateless: nothing carries over between calls.
/// Geo's and NonAI Weather's own inner tool calls (e.g. Geo calling GetLatLong) run inside the
/// non-streamed async call AsAIFunction generates (<c>InvokeAgentAsync</c>) and do not surface as
/// separate SSE events — only the orchestrator's delegation calls to Geo and NonAI Weather do.
/// For the same reason, Geo's and NonAI Weather's own model token usage never reaches
/// <see cref="ChatUsageAccumulator"/> (only the orchestrator's own <see cref="AgentResponseUpdate"/>
/// contents do) — the usage chip a user sees undercounts a multi-agent turn.
/// </summary>
public sealed class Chat4aService : IChatClientService
{
    private readonly IChatSessionStore _sessionStore;
    private readonly ChatAgentSessionStore _agentSessionStore;
    private readonly IMediator _mediator;
    private readonly ChatFoundrySettings _settings;
    private readonly ILogger<Chat4aService> _logger;

    public Chat4aService(
        IChatSessionStore sessionStore,
        ChatAgentSessionStore agentSessionStore,
        IMediator mediator,
        ChatFoundrySettings settings,
        ILogger<Chat4aService> logger)
    {
        _sessionStore = sessionStore;
        _agentSessionStore = agentSessionStore;
        _mediator = mediator;
        _settings = settings;
        _logger = logger;
    }

    public async IAsyncEnumerable<ChatStreamEvent> SendMessageAsync(
        ChatSendMessageRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var sessionId = ChatResponsesSessionHelper.ResolveSessionId(
            _sessionStore,
            ChatResponsesSessionHelper.Chat4aKind,
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
        AIAgent orchestrationAgent = BuildOrchestrationAgent(responsesClient);

        AgentSession? agentSession = null;
        string? sessionError = null;
        try
        {
            agentSession = await _agentSessionStore.GetOrCreateAsync(orchestrationAgent, sessionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat4a failed to create agent session");
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
            updates = orchestrationAgent.RunStreamingAsync(userMessage, agentSession, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat4a failed to start streaming");
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
                    Description = "Geo assistant. Resolves a location name to latitude/longitude, or reverse-geocodes latitude/longitude to a place label. Send it a natural-language geo question; it returns the answer as text.",
                }),
                // session omitted — AsAIFunction creates a fresh, throwaway session per call, so NonAI
                // Weather is stateless per delegated call; the orchestrator alone owns memory.
                nonAiWeatherAgent.AsAIFunction(new AIFunctionFactoryOptions
                {
                    Name = "NonAIWeather",
                    Description = "Weather assistant. Reports current conditions, forecast, or recent history for a latitude/longitude. Accepts numeric coordinates only — resolve a place name to coordinates via Geo first. It has no memory of its own, so include the coordinates on every call, including follow-up turns. Send it a natural-language weather question that names the coordinates; it returns the answer as text.",
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
