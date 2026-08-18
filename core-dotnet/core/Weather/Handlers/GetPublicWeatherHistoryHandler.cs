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
/// Fetches recent past public weather from Open-Meteo for a given lat/long.
/// Omits unit query params so Open-Meteo returns its defaults (°C, km/h, mm);
/// the AI converts to US customary units.
/// </summary>
public class GetPublicWeatherHistoryHandler : IRequestHandler<GetPublicWeatherHistoryEvent, PublicWeatherHistoryResponse>
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

    public async Task<PublicWeatherHistoryResponse> Handle(GetPublicWeatherHistoryEvent request, CancellationToken cancellationToken)
    {
        var cacheKey = JsonSerializer.Serialize(new { Handler = nameof(GetPublicWeatherHistoryHandler), Request = request });
        return await _cache.GetOrCreate(
            cacheKey: cacheKey,
            cacheDuration: TimeSpan.FromMinutes(5),
            valueFactory: ct => _retry.Execute(c => GetPublicWeatherHistory(request, c), ct),
            cancellationToken: cancellationToken);
    }

    private async Task<PublicWeatherHistoryResponse> GetPublicWeatherHistory(GetPublicWeatherHistoryEvent request, CancellationToken cancellationToken)
    {
        using var client = _clientFactory.CreateClient();
        string endpoint = BuildHistoryUrl(request.Latitude, request.Longitude, request.Resolution);

        string jsonResponse = await client.GetStringAsync(endpoint, cancellationToken);

        PublicWeatherHistoryResponse weatherData = JsonSerializer.Deserialize<PublicWeatherHistoryResponse>(jsonResponse)
            ?? throw new InvalidOperationException("Non-AI: Weather history API returned empty or invalid JSON.");

        NormalizeNullCollections(weatherData);

        return weatherData;
    }

    /// <summary>
    /// Open-Meteo can serialize a series field as JSON null instead of omitting it. This response
    /// gets cached and read by multiple consumers (the UI mapper and MCP tools), so normalize any
    /// null list to empty here, once, rather than null-checking at every read site.
    /// </summary>
    private static void NormalizeNullCollections(PublicWeatherHistoryResponse response)
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
            $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&{query}&timezone=auto");
    }
}
