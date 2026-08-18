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
/// Requests Celsius, km/h, and mm explicitly; the AI converts to US customary units.
/// </summary>
public class GetPublicWeatherForecastHandler : IRequestHandler<GetPublicWeatherForecastEvent, NonAIForecastWeatherResponse>
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

    public async Task<NonAIForecastWeatherResponse> Handle(GetPublicWeatherForecastEvent request, CancellationToken cancellationToken)
    {
        var cacheKey = JsonSerializer.Serialize(new { Handler = nameof(GetPublicWeatherForecastHandler), Request = request });
        return await _cache.GetOrCreate(
            cacheKey: cacheKey,
            cacheDuration: TimeSpan.FromMinutes(5),
            valueFactory: ct => _retry.Execute(c => GetPublicWeatherForecast(request, c), ct),
            cancellationToken: cancellationToken);
    }

    private async Task<NonAIForecastWeatherResponse> GetPublicWeatherForecast(GetPublicWeatherForecastEvent request, CancellationToken cancellationToken)
    {
        using var client = _clientFactory.CreateClient();
        string endpoint = BuildForecastUrl(request.Latitude, request.Longitude, request.Resolution);

        string jsonResponse = await client.GetStringAsync(endpoint, cancellationToken);

        NonAIForecastWeatherResponse weatherData = JsonSerializer.Deserialize<NonAIForecastWeatherResponse>(jsonResponse)
            ?? throw new InvalidOperationException("Non-AI: Weather forecast API returned empty or invalid JSON.");

        NormalizeNullCollections(weatherData);

        return weatherData;
    }

    /// <summary>
    /// Open-Meteo can serialize a series field as JSON null instead of omitting it. This response
    /// gets cached and read by multiple consumers (the UI mapper and MCP tools), so normalize any
    /// null list to empty here, once, rather than null-checking at every read site. Also clamps any
    /// negative precipitation reading (an Open-Meteo sensor/interpolation artifact) to zero.
    /// </summary>
    private static void NormalizeNullCollections(NonAIForecastWeatherResponse response)
    {
        if (response.Hourly is { } hourly)
        {
            hourly.Time ??= [];
            hourly.Temperature2mC ??= [];
            hourly.PrecipitationMm ??= [];
            hourly.WeatherCode ??= [];
            hourly.WindSpeed10mKmh ??= [];
            hourly.WindDirectionSource10m ??= [];
            ClampNegativeToZero(hourly.PrecipitationMm);
        }

        if (response.Daily is { } daily)
        {
            daily.Time ??= [];
            daily.WeatherCode ??= [];
            daily.Temperature2mMaxC ??= [];
            daily.Temperature2mMinC ??= [];
            daily.PrecipitationSumMm ??= [];
            daily.WindSpeed10mMaxKmh ??= [];
            daily.WindDirectionSource10mDominant ??= [];
            ClampNegativeToZero(daily.PrecipitationSumMm);
        }

        if (response.Minutely15 is { } minutely15)
        {
            minutely15.Time ??= [];
            minutely15.Temperature2mC ??= [];
            minutely15.PrecipitationMm ??= [];
            minutely15.WeatherCode ??= [];
            minutely15.WindSpeed10mKmh ??= [];
            minutely15.WindDirectionSource10m ??= [];
            ClampNegativeToZero(minutely15.PrecipitationMm);
        }
    }

    private static void ClampNegativeToZero(List<double> values)
    {
        for (var i = 0; i < values.Count; i++)
        {
            if (values[i] < 0)
            {
                values[i] = 0;
            }
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
            $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&{query}&{OpenMeteoUnits.CelsiusKmhMm}&timezone=auto");
    }
}
