using System.Globalization;
using System.Text.Json;
using Core.Caching;
using Core.Http;
using Core.Weather.Events;
using Core.Weather.Models;
using CQMediator;
using Microsoft.Extensions.Http;

namespace Core.Weather.Handlers;

/// <summary>
/// Fetches recent past public weather from Open-Meteo for a given lat/long.
/// Requests Celsius, km/h, and mm explicitly; the AI converts to US customary units.
/// </summary>
public class GetPublicWeatherHistoryHandler : IRequestHandler<GetPublicWeatherHistoryEvent, NonAIHistoryWeatherResponse>
{
    private readonly CacheHelper _cache;
    private readonly TransientRetryHelper _retry;
    private readonly IHttpClientFactory _clientFactory;

    public GetPublicWeatherHistoryHandler(
        CacheHelper cache,
        TransientRetryHelper retry,
        IHttpClientFactory clientFactory)
    {
        _cache = cache;
        _retry = retry;
        _clientFactory = clientFactory;
    }

    public async Task<NonAIHistoryWeatherResponse> Handle(GetPublicWeatherHistoryEvent request, CancellationToken cancellationToken)
    {
        var cacheKey = JsonSerializer.Serialize(new { Handler = nameof(GetPublicWeatherHistoryHandler), Request = request });
        return await _cache.GetOrCreate(
            cacheKey: cacheKey,
            cacheDuration: TimeSpan.FromMinutes(5),
            valueFactory: ct => _retry.Execute(c => GetPublicWeatherHistory(request, c), ct),
            cancellationToken: cancellationToken);
    }

    private async Task<NonAIHistoryWeatherResponse> GetPublicWeatherHistory(GetPublicWeatherHistoryEvent request, CancellationToken cancellationToken)
    {
        using var client = _clientFactory.CreateClient();
        string endpoint = BuildHistoryUrl(request.Latitude, request.Longitude, request.Resolution);

        string jsonResponse = await client.GetStringAsync(endpoint, cancellationToken);

        NonAIHistoryWeatherResponse weatherData = JsonSerializer.Deserialize<NonAIHistoryWeatherResponse>(jsonResponse)
            ?? throw new InvalidOperationException("Non-AI: Weather history API returned empty or invalid JSON.");

        NormalizeNullCollections(weatherData);

        return weatherData;
    }

    /// <summary>
    /// Open-Meteo can serialize a series field as JSON null instead of omitting it. This response
    /// gets cached and read by multiple consumers (the UI mapper and MCP tools), so normalize any
    /// null list to empty here, once, rather than null-checking at every read site. Also clamps any
    /// negative precipitation reading (an Open-Meteo sensor/interpolation artifact) to zero.
    /// </summary>
    private static void NormalizeNullCollections(NonAIHistoryWeatherResponse response)
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
            $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&{query}&{OpenMeteoUnits.CelsiusKmhMm}&timezone=auto");
    }
}
