using System.Text.Json;
using Core.http;
using Core.weather.Events;
using Core.weather.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Core.weather.Handlers;

/// <summary>
/// Fetches public current-weather data from Open-Meteo for a given lat/long.
/// </summary>
public class GetPublicWeatherDataHandler : IRequestHandler<GetPublicWeatherDataEvent, NonAIWeatherResponse>
{
    private readonly ILogger<GetPublicWeatherDataHandler> _logger;

    public GetPublicWeatherDataHandler(ILogger<GetPublicWeatherDataHandler> logger)
    {
        _logger = logger;
    }

    public async Task<NonAIWeatherResponse> Handle(GetPublicWeatherDataEvent request, CancellationToken cancellationToken)
    {
        var currentWeatherPath = "forecast";

        string url = $"https://api.open-meteo.com/v1/{currentWeatherPath}?latitude={request.LatLong.Latitude}&longitude={request.LatLong.Longitude}&current_weather=true";

        var options = new JsonSerializerOptions { WriteIndented = true };

        NonAIWeatherResponse weatherData = await OpenMeteoJsonClient.GetAsync<NonAIWeatherResponse>(url, _logger, cancellationToken, options)
            ?? throw new InvalidOperationException("Non-AI: Weather API returned empty or invalid JSON.");

        return weatherData;
    }
}
