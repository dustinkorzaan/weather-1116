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
/// Fetches public current-weather data from Open-Meteo for a given lat/long.
/// Requests Celsius and km/h explicitly; the AI converts to US customary units.
/// </summary>
public class GetPublicWeatherCurrentHandler : IRequestHandler<GetPublicWeatherCurrentEvent, NonAICurrentWeatherResponse>
{
    private readonly CacheHelper _cache;
    private readonly TransientRetryHelper _retry;
    private readonly IHttpClientFactory _clientFactory;

    public GetPublicWeatherCurrentHandler(
        CacheHelper cache,
        TransientRetryHelper retry,
        IHttpClientFactory clientFactory)
    {
        _cache = cache;
        _retry = retry;
        _clientFactory = clientFactory;
    }

    public async Task<NonAICurrentWeatherResponse> Handle(GetPublicWeatherCurrentEvent request, CancellationToken cancellationToken)
    {
        var cacheKey = JsonSerializer.Serialize(new { Handler = nameof(GetPublicWeatherCurrentHandler), Request = request });
        return await _cache.GetOrCreate(
            cacheKey: cacheKey,
            cacheDuration: TimeSpan.FromMinutes(5),
            valueFactory: ct => _retry.Execute(c => GetPublicWeatherCurrent(request, c), ct),
            cancellationToken: cancellationToken);
    }

    private async Task<NonAICurrentWeatherResponse> GetPublicWeatherCurrent(GetPublicWeatherCurrentEvent request, CancellationToken cancellationToken)
    {
        using var client = _clientFactory.CreateClient();
        string endpoint = BuildCurrentWeatherUrl(request.Latitude, request.Longitude);

        string jsonResponse = await client.GetStringAsync(endpoint, cancellationToken);

        NonAICurrentWeatherResponse weatherData = JsonSerializer.Deserialize<NonAICurrentWeatherResponse>(jsonResponse)
            ?? throw new InvalidOperationException("Non-AI: Weather API returned empty or invalid JSON.");

        return weatherData;
    }

    internal static string BuildCurrentWeatherUrl(double latitude, double longitude) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current_weather=true&{OpenMeteoUnits.CelsiusKmh}");
}
