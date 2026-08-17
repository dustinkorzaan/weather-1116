using System.Globalization;
using System.Text.Json;
using Core.Http.Events;
using Core.Weather.Events;
using Core.Weather.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Core.Weather.Handlers;

/// <summary>
/// Fetches an upcoming public weather forecast from Open-Meteo for a given lat/long.
/// </summary>
public class GetPublicWeatherForecastHandler : IRequestHandler<GetPublicWeatherForecastEvent, PublicWeatherForecastResponse>
{
    private readonly IMediator _mediator;
    private readonly ILogger<GetPublicWeatherForecastHandler> _logger;

    public GetPublicWeatherForecastHandler(
        IMediator mediator,
        ILogger<GetPublicWeatherForecastHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<PublicWeatherForecastResponse> Handle(GetPublicWeatherForecastEvent request, CancellationToken cancellationToken)
    {
        string endpoint = BuildForecastUrl(request.Latitude, request.Longitude, request.Resolution);

        string jsonResponse = await _mediator.Send(
            new GetCachedThirdPartyStringWithRetryEvent { RequestUri = endpoint },
            cancellationToken);

        var options = new JsonSerializerOptions { WriteIndented = true };

        PublicWeatherForecastResponse weatherData = JsonSerializer.Deserialize<PublicWeatherForecastResponse>(jsonResponse, options)
            ?? throw new InvalidOperationException("Non-AI: Weather forecast API returned empty or invalid JSON.");

        return weatherData;
    }

    internal static string BuildForecastUrl(
        double latitude,
        double longitude,
        PublicWeatherForecastResolution resolution)
    {
        var query = resolution switch
        {
            PublicWeatherForecastResolution.Daily =>
                "daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_sum,wind_speed_10m_max,wind_direction_10m_dominant&forecast_days=7",
            PublicWeatherForecastResolution.Hourly =>
                "hourly=temperature_2m,precipitation,weather_code,wind_speed_10m,wind_direction_10m&forecast_hours=48",
            PublicWeatherForecastResolution.FifteenMinutes =>
                "minutely_15=temperature_2m,precipitation,weather_code,wind_speed_10m,wind_direction_10m&forecast_minutely_15=192",
            _ => throw new ArgumentOutOfRangeException(nameof(resolution), resolution, "Unsupported forecast resolution."),
        };

        return string.Create(
            CultureInfo.InvariantCulture,
            $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&{query}&timezone=auto&temperature_unit=fahrenheit&wind_speed_unit=mph");
    }
}
