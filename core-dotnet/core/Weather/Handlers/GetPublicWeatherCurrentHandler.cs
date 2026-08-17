using System.Globalization;
using System.Text.Json;
using Core.Caching;
using Core.Http;
using Core.Weather.Events;
using Core.Weather.Models;
using MediatR;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Core.Weather.Handlers;

/// <summary>
/// Fetches public current-weather data from Open-Meteo for a given lat/long.
/// </summary>
public class GetPublicWeatherCurrentHandler : IRequestHandler<GetPublicWeatherCurrentEvent, NonAIWeatherResponse>
{
    private readonly CacheHelper _cache;
    private readonly TransientRetryHelper _retry;
    private readonly IHttpClientFactory _clientFactory;
    private readonly ILogger<GetPublicWeatherCurrentHandler> _logger;

    public GetPublicWeatherCurrentHandler(
        CacheHelper cache,
        TransientRetryHelper retry,
        IHttpClientFactory clientFactory,
        ILogger<GetPublicWeatherCurrentHandler> logger)
    {
        _cache = cache;
        _retry = retry;
        _clientFactory = clientFactory;
        _logger = logger;
    }

    public async Task<NonAIWeatherResponse> Handle(GetPublicWeatherCurrentEvent request, CancellationToken cancellationToken)
    {
        var cacheKey = JsonSerializer.Serialize(new { Handler = nameof(GetPublicWeatherCurrentHandler), Request = request });
        return await _cache.GetOrCreate(
            cacheKey: cacheKey,
            cacheDuration: TimeSpan.FromMinutes(5),
            valueFactory: ct => _retry.ExecuteAsync(c => GetPublicWeatherCurrent(request, c), ct),
            cancellationToken: cancellationToken);
    }

    private async Task<NonAIWeatherResponse> GetPublicWeatherCurrent(GetPublicWeatherCurrentEvent request, CancellationToken cancellationToken)
    {
        using var client = _clientFactory.CreateClient();
        string endpoint = BuildCurrentWeatherUrl(request.Latitude, request.Longitude);

        string jsonResponse = await client.GetStringAsync(endpoint, cancellationToken);

        NonAIWeatherResponse weatherData = JsonSerializer.Deserialize<NonAIWeatherResponse>(jsonResponse)
            ?? throw new InvalidOperationException("Non-AI: Weather API returned empty or invalid JSON.");

        return weatherData;
    }

    internal static string BuildCurrentWeatherUrl(double latitude, double longitude) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current_weather=true");
}
