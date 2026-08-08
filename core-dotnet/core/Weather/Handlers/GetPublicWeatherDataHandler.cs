using System.Text.Json;
using Core.Weather.Events;
using Core.Weather.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Core.Weather.Handlers;

/// <summary>
/// Fetches public current-weather data from Open-Meteo for a given lat/long.
/// </summary>
public class GetPublicWeatherDataHandler : IRequestHandler<GetPublicWeatherDataEvent, NonAIWeatherResponse>
{
    private readonly ILogger<GetPublicWeatherDataHandler> _logger;

    public GetPublicWeatherDataHandler(ILogger<GetPublicWeatherDataHandler> logger)
    {
        _logger = logger;
    }

    public async Task<NonAIWeatherResponse> Handle(GetPublicWeatherDataEvent request, CancellationToken cancellationToken)
    {
        var client = new HttpClient();
        var currentWeatherPath = "forecast";

        string endpoint = $"https://api.open-meteo.com/v1/{currentWeatherPath}?latitude={request.LatLong.Latitude}&longitude={request.LatLong.Longitude}&current_weather=true";
        // _logger.LogInformation("Non-AI: Fetching weather data from: {endpoint}", endpoint);

        // 1. Fetch raw JSON string from API
        string jsonResponse = await client.GetStringAsync(endpoint, cancellationToken);

        // 2. Options to format the console output nicely
        var options = new JsonSerializerOptions { WriteIndented = true };

        // 3. Deserialize into the C# Class Model
        NonAIWeatherResponse weatherData = JsonSerializer.Deserialize<NonAIWeatherResponse>(jsonResponse, options)
            ?? throw new InvalidOperationException("Non-AI: Weather API returned empty or invalid JSON.");

        // 4. Return deserialized weather data
        return weatherData;
    }
}
