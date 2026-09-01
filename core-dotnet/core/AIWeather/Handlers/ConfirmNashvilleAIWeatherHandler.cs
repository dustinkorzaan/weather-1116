using Core.AIWeather.Events;
using Core.AIWeather.Models;
using CQMediator;
using Microsoft.Extensions.Logging;

namespace Core.AIWeather.Handlers;

/// <summary>
/// Fetches AI weather for Nashville, TN and validates the response.
/// </summary>
public class ConfirmNashvilleAIWeatherHandler : IRequestHandler<ConfirmNashvilleAIWeatherEvent, AIWeatherResponse>
{
    private const string Location = "Nashville, TN";

    private readonly IMediator _mediator;
    private readonly ILogger<ConfirmNashvilleAIWeatherHandler> _logger;

    public ConfirmNashvilleAIWeatherHandler(
        IMediator mediator,
        ILogger<ConfirmNashvilleAIWeatherHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<AIWeatherResponse> Handle(
        ConfirmNashvilleAIWeatherEvent request,
        CancellationToken cancellationToken)
    {
        var response = request.Version == 3
            ? await _mediator.Send(new GetCurrentAIWeatherV3Event { Location = Location }, cancellationToken)
            : await _mediator.Send(new GetCurrentAIWeatherV4Event { Location = Location }, cancellationToken);

        ConfirmResponse(response);

        _logger.LogInformation(
            "Confirmed AI weather V{Version} for {Location}: {Summary}",
            request.Version,
            Location,
            response.FullSummary);

        return response;
    }

    private static void ConfirmResponse(AIWeatherResponse response)
    {
        if (string.IsNullOrWhiteSpace(response.FullSummary))
        {
            throw new InvalidOperationException("AI weather response is missing fullSummary.");
        }
    }
}
