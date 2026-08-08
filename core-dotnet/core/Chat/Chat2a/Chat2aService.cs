using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Core.Chat.Models;
using Core.Chat.Services;
using Core.Geo.Events;
using Core.Geo.Models;
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

        var userMessage = request.Message.Trim();
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
                if (content is FunctionCallContent functionCall)
                {
                    yield return ChatStreamEvent.ToolStart(functionCall.Name);
                    yield return ChatStreamEvent.ToolEnd(functionCall.Name);
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

    private IList<AITool> CreateTools() =>
    [
        AIFunctionFactory.Create(GetLatLongDataAsync),
        AIFunctionFactory.Create(GetPublicWeatherDataAsync),
    ];

    [Description("Resolve a location name to latitude and longitude using public geocoding data.")]
    private async Task<string> GetLatLongDataAsync(
        [Description("City and optional region/country, e.g. Nashville, TN")] string location,
        CancellationToken cancellationToken)
    {
        var latLong = await _mediator.Send(new GetLatLongDataEvent { Location = location }, cancellationToken);
        return JsonSerializer.Serialize(latLong, new JsonSerializerOptions { WriteIndented = true });
    }

    [Description("Get current public weather conditions for a latitude and longitude.")]
    private async Task<string> GetPublicWeatherDataAsync(
        [Description("Latitude in decimal degrees")] double latitude,
        [Description("Longitude in decimal degrees")] double longitude,
        CancellationToken cancellationToken)
    {
        var weatherData = await _mediator.Send(new GetPublicWeatherDataEvent
        {
            LatLong = new NonAILatLongResponse
            {
                Latitude = latitude,
                Longitude = longitude,
            },
        }, cancellationToken);

        return JsonSerializer.Serialize(weatherData, new JsonSerializerOptions { WriteIndented = true });
    }
}
