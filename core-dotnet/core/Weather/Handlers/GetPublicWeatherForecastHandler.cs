using System.Globalization;
using System.Text.Json;
using Core.Caching;
using Core.Http;
using Core.Weather.Events;
using Core.Weather.Models;
using MediatR;
using Microsoft.Extensions.Http;

namespace Core.Weather.Handlers;

/// <summary>
/// Fetches an upcoming public weather forecast from Open-Meteo for a given lat/long.
/// </summary>
public class GetPublicWeatherForecastHandler : IRequestHandler<GetPublicWeatherForecastEvent, PublicWeatherForecastResponse>
{
    private readonly CacheHelper _cache;
    private readonly TransientRetryHelper _retry;
    private readonly IHttpClientFactory _clientFactory;

    public GetPublicWeatherForecastHandler(
        CacheHelper cache,
        TransientRetryHelper retry,
        IHttpClientFactory clientFactory)
    {
        _cache = cache;
        _retry = retry;
        _clientFactory = clientFactory;
    }

    public async Task<PublicWeatherForecastResponse> Handle(GetPublicWeatherForecastEvent request, CancellationToken cancellationToken)
    {
        var cacheKey = JsonSerializer.Serialize(new { Handler = nameof(GetPublicWeatherForecastHandler), Request = request });
        return await _cache.GetOrCreate(
            cacheKey: cacheKey,
            cacheDuration: TimeSpan.FromMinutes(5),
            valueFactory: ct => _retry.Execute(c => GetPublicWeatherForecast(request, c), ct),
            cancellationToken: cancellationToken);
    }

    private async Task<PublicWeatherForecastResponse> GetPublicWeatherForecast(GetPublicWeatherForecastEvent request, CancellationToken cancellationToken)
    {
        using var client = _clientFactory.CreateClient();
        string endpoint = BuildForecastUrl(request.Latitude, request.Longitude, request.Resolution);

        string jsonResponse = await client.GetStringAsync(endpoint, cancellationToken);

        PublicWeatherForecastResponse weatherData = JsonSerializer.Deserialize<PublicWeatherForecastResponse>(jsonResponse)
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
            $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&{query}&timezone=auto&temperature_unit=fahrenheit&wind_speed_unit=mph&precipitation_unit=inch");
    }
}
