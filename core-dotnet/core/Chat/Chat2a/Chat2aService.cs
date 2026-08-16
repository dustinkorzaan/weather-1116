using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Core.Chat.Models;
using Core.Chat.Services;
using Core.Geo.Events;
using Core.Weather.Events;
using MediatR;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI.Responses;

namespace Core.Chat.Chat2a;

public sealed class Chat2aService : IChatClientService
{
    private readonly IChatSessionStore _sessionStore;
    private readonly ChatAgentSessionStore _agentSessionStore;
    private readonly IMediator _mediator;
    private readonly ChatFoundrySettings _settings;
    private readonly ILogger<Chat2aService> _logger;

    public Chat2aService(
        IChatSessionStore sessionStore,
        ChatAgentSessionStore agentSessionStore,
        IMediator mediator,
        ChatFoundrySettings settings,
        ILogger<Chat2aService> logger)
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
            ChatResponsesSessionHelper.Chat2aKind,
            request.SessionId);

        yield return ChatStreamEvent.Session(sessionId);

        var userMessage = request.Message?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            yield return ChatStreamEvent.Error("Message cannot be empty.");
            yield break;
        }

        _sessionStore.AppendMessage(sessionId, new Models.ChatMessage { Role = "user", Content = userMessage });

        var responsesClient = _settings.CreateResponsesClient();
        AIAgent agent = responsesClient.AsAIAgent(
            name: "Chat2a",
            instructions: ChatSystemInstructions.WeatherAssistant,
            model: _settings.DeploymentName,
            tools: CreateTools());

        AgentSession? agentSession = null;
        string? sessionError = null;
        try
        {
            agentSession = await _agentSessionStore.GetOrCreateAsync(agent, sessionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat2a failed to create agent session");
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
            updates = agent.RunStreamingAsync(userMessage, agentSession, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat2a failed to start streaming");
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

        yield return ChatStreamEvent.Done();
    }

    private sealed record PendingToolCall(string Name, string? Arguments);

    private IList<AITool> CreateTools() =>
    [
        AIFunctionFactory.Create(GetLatLongData),
        AIFunctionFactory.Create(GetLocationData),
        AIFunctionFactory.Create(GetPublicWeatherCurrent),
        AIFunctionFactory.Create(GetPublicWeatherForecast),
        AIFunctionFactory.Create(GetPublicWeatherHistory),
    ];

    [Description("Resolve a location name to ranked latitude/longitude matches using public geocoding data. Returns up to 5 results (rank 1 is the best match). Use state and country to pick the right place if rank 1 is wrong.")]
    private async Task<string> GetLatLongData(
        [Description("City and optional region/country, e.g. Nashville, TN")] string location,
        CancellationToken cancellationToken)
    {
        var latLongMatches = await _mediator.Send(new GetLatLongDataEvent { Location = location }, cancellationToken);
        return JsonSerializer.Serialize(latLongMatches, new JsonSerializerOptions { WriteIndented = true });
    }

    [Description("Turn a latitude and longitude into a simple place label. US results are City, State; elsewhere City, State, Country.")]
    private async Task<string> GetLocationData(
        [Description("Latitude in decimal degrees")] double latitude,
        [Description("Longitude in decimal degrees")] double longitude,
        CancellationToken cancellationToken)
    {
        var locationData = await _mediator.Send(new GetLocationDataEvent
        {
            Latitude = latitude,
            Longitude = longitude,
        }, cancellationToken);
        return JsonSerializer.Serialize(locationData, new JsonSerializerOptions { WriteIndented = true });
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

        return JsonSerializer.Serialize(weatherData, new JsonSerializerOptions { WriteIndented = true });
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

        return JsonSerializer.Serialize(weatherData, new JsonSerializerOptions { WriteIndented = true });
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

        return JsonSerializer.Serialize(weatherData, new JsonSerializerOptions { WriteIndented = true });
    }
}
