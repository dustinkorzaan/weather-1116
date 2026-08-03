using Core.AIWeather.Events;
using Core.AIWeather.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Core.AIWeather.Handlers;

/// <summary>
/// Fetches AI weather for Nashville, TN, validates the response, then fails intentionally
/// so Hangfire records a failed job for health monitoring.
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
        var response = await _mediator.Send(
            new GetCurrentAIWeatherEvent { Location = Location },
            cancellationToken);

        ConfirmResponse(response);

        _logger.LogInformation(
            "Confirmed AI weather for {Location}: {Summary}",
            Location,
            response.FullSummary);

        throw new InvalidOperationException(
            $"Nashville AI weather probe confirmed a response but failed intentionally for Hangfire health monitoring.");
    }

    private static void ConfirmResponse(AIWeatherResponse response)
    {
        if (string.IsNullOrWhiteSpace(response.FullSummary))
        {
            throw new InvalidOperationException("AI weather response is missing fullSummary.");
        }

        if (string.IsNullOrWhiteSpace(response.Conditions))
        {
            throw new InvalidOperationException("AI weather response is missing conditions.");
        }
    }
}
