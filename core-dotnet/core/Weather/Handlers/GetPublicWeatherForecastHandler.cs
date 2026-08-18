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
/// Omits unit query params so Open-Meteo returns its defaults (°C, km/h, mm);
/// the AI converts to US customary units.
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

        NormalizeNullCollections(weatherData);

        return weatherData;
    }

    /// <summary>
    /// Open-Meteo can serialize a series field as JSON null instead of omitting it. This response
    /// gets cached and read by multiple consumers (the UI mapper and MCP tools), so normalize any
    /// null list to empty here, once, rather than null-checking at every read site.
    /// </summary>
    private static void NormalizeNullCollections(PublicWeatherForecastResponse response)
    {
        if (response.Hourly is { } hourly)
        {
            hourly.Time ??= [];
            hourly.Temperature2m ??= [];
            hourly.Precipitation ??= [];
            hourly.WeatherCode ??= [];
            hourly.WindSpeed10m ??= [];
            hourly.WindDirection10m ??= [];
        }

        if (response.Daily is { } daily)
        {
            daily.Time ??= [];
            daily.WeatherCode ??= [];
            daily.Temperature2mMax ??= [];
            daily.Temperature2mMin ??= [];
            daily.PrecipitationSum ??= [];
            daily.WindSpeed10mMax ??= [];
            daily.WindDirection10mDominant ??= [];
        }

        if (response.Minutely15 is { } minutely15)
        {
            minutely15.Time ??= [];
            minutely15.Temperature2m ??= [];
            minutely15.Precipitation ??= [];
            minutely15.WeatherCode ??= [];
            minutely15.WindSpeed10m ??= [];
            minutely15.WindDirection10m ??= [];
        }
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
            $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&{query}&timezone=auto");
    }
}
