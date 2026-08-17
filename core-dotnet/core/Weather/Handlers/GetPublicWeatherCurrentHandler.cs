using System.Globalization;
using System.Text.Json;
using Core.Http.Events;
using Core.Weather.Events;
using Core.Weather.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Core.Weather.Handlers;

/// <summary>
/// Fetches public current-weather data from Open-Meteo for a given lat/long.
/// </summary>
public class GetPublicWeatherCurrentHandler : IRequestHandler<GetPublicWeatherCurrentEvent, NonAIWeatherResponse>
{
    private readonly IMediator _mediator;
    private readonly ILogger<GetPublicWeatherCurrentHandler> _logger;

    public GetPublicWeatherCurrentHandler(
        IMediator mediator,
        ILogger<GetPublicWeatherCurrentHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<NonAIWeatherResponse> Handle(GetPublicWeatherCurrentEvent request, CancellationToken cancellationToken)
    {
        string endpoint = BuildCurrentWeatherUrl(request.Latitude, request.Longitude);

        string jsonResponse = await _mediator.Send(
            new GetThirdPartyStringWithRetryEvent { RequestUri = endpoint },
            cancellationToken);

        var options = new JsonSerializerOptions { WriteIndented = true };

        NonAIWeatherResponse weatherData = JsonSerializer.Deserialize<NonAIWeatherResponse>(jsonResponse, options)
            ?? throw new InvalidOperationException("Non-AI: Weather API returned empty or invalid JSON.");

        return weatherData;
    }

    internal static string BuildCurrentWeatherUrl(double latitude, double longitude) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current_weather=true");
}
