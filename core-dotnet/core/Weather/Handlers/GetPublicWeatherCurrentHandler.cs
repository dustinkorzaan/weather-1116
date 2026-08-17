using System.Globalization;
using System.Text.Json;
using Core.Http;
using Core.Weather.Events;
using Core.Weather.Models;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Core.Weather.Handlers;

/// <summary>
/// Fetches public current-weather data from Open-Meteo for a given lat/long.
/// </summary>
public class GetPublicWeatherCurrentHandler : IRequestHandler<GetPublicWeatherCurrentEvent, NonAIWeatherResponse>
{
    internal const int RetryCount = 5;
    internal const int RetryDelay = 200;

    private readonly IMemoryCache _cache;
    private readonly ILogger<GetPublicWeatherCurrentHandler> _logger;

    public GetPublicWeatherCurrentHandler(IMemoryCache cache, ILogger<GetPublicWeatherCurrentHandler> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<NonAIWeatherResponse> Handle(GetPublicWeatherCurrentEvent request, CancellationToken cancellationToken)
    {
        var cacheKey = JsonSerializer.Serialize(new { Handler = nameof(GetPublicWeatherCurrentHandler), Request = request });

        if (_cache.TryGetValue(cacheKey, out NonAIWeatherResponse? cached))
        {
            return cached!;
        }

        for (var attempt = 0; ; attempt++)
        {
            try
            {
                var result = await GetPublicWeatherCurrent(request, cancellationToken);
                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
                return result;
            }
            catch when (attempt < RetryCount)
            {
                await Task.Delay((int)(RetryDelay * Math.Pow(2, attempt)), cancellationToken);
            }
        }
    }

    private static async Task<NonAIWeatherResponse> GetPublicWeatherCurrent(GetPublicWeatherCurrentEvent request, CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        string endpoint = BuildCurrentWeatherUrl(request.Latitude, request.Longitude);

        string jsonResponse = await ThirdPartyHttp.GetStringWithRetryAsync(client, endpoint, cancellationToken);

        var options = new JsonSerializerOptions { WriteIndented = true };

        NonAIWeatherResponse weatherData = JsonSerializer.Deserialize<NonAIWeatherResponse>(jsonResponse, options)
            ?? throw new InvalidOperationException("Non-AI: Weather API returned empty or invalid JSON.");

        return weatherData;
    }

    internal static string BuildCurrentWeatherUrl(double latitude, double longitude) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current_weather=true");
}
