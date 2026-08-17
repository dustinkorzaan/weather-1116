using System.Globalization;
using System.Text.Json;
using Core.Http.Events;
using Core.Weather.Events;
using Core.Weather.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Core.Weather.Handlers;

/// <summary>
/// Fetches recent past public weather from Open-Meteo for a given lat/long.
/// </summary>
public class GetPublicWeatherHistoryHandler : IRequestHandler<GetPublicWeatherHistoryEvent, PublicWeatherHistoryResponse>
{
    private readonly IMediator _mediator;
    private readonly ILogger<GetPublicWeatherHistoryHandler> _logger;

    public GetPublicWeatherHistoryHandler(
        IMediator mediator,
        ILogger<GetPublicWeatherHistoryHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<PublicWeatherHistoryResponse> Handle(GetPublicWeatherHistoryEvent request, CancellationToken cancellationToken)
    {
        string endpoint = BuildHistoryUrl(request.Latitude, request.Longitude, request.Resolution);

        string jsonResponse = await _mediator.Send(
            new GetThirdPartyStringWithRetryEvent { RequestUri = endpoint },
            cancellationToken);

        var options = new JsonSerializerOptions { WriteIndented = true };

        PublicWeatherHistoryResponse weatherData = JsonSerializer.Deserialize<PublicWeatherHistoryResponse>(jsonResponse, options)
            ?? throw new InvalidOperationException("Non-AI: Weather history API returned empty or invalid JSON.");

        return weatherData;
    }

    internal static string BuildHistoryUrl(
        double latitude,
        double longitude,
        PublicWeatherHistoryResolution resolution)
    {
        var query = resolution switch
        {
            PublicWeatherHistoryResolution.Daily =>
                "daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_sum,wind_speed_10m_max,wind_direction_10m_dominant&past_days=7&forecast_days=0",
            PublicWeatherHistoryResolution.Hourly =>
                "hourly=temperature_2m,precipitation,weather_code,wind_speed_10m,wind_direction_10m&past_hours=48&forecast_hours=0",
            _ => throw new ArgumentOutOfRangeException(nameof(resolution), resolution, "Unsupported history resolution."),
        };

        return string.Create(
            CultureInfo.InvariantCulture,
            $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&{query}&timezone=auto&temperature_unit=fahrenheit&wind_speed_unit=mph");
    }
}
