using System.Text.Json;
using Core.weather.Events;
using Core.weather.Models;
using MediatR;

namespace Core.weather.Handlers;

/// <summary>
/// Fetches public current-weather data from Open-Meteo for a given lat/long.
/// </summary>
public class GetPublicWeatherDataHandler : IRequestHandler<GetPublicWeatherDataEvent, NonAIWeatherResponse>
{
    public async Task<NonAIWeatherResponse> Handle(GetPublicWeatherDataEvent request, CancellationToken cancellationToken)
    {
        var client = new HttpClient();
        var currentWeatherPath = "forecast";

        string url = $"https://api.open-meteo.com/v1/{currentWeatherPath}?latitude={request.LatLong.Latitude}&longitude={request.LatLong.Longitude}&current_weather=true";
        Console.WriteLine($"Non-AI: Fetching weather data from: {url}");

        try
        {
            // 1. Fetch raw JSON string from API
            string jsonResponse = await client.GetStringAsync(url, cancellationToken);

            // 2. Options to format the console output nicely
            var options = new JsonSerializerOptions { WriteIndented = true };

            // 3. Deserialize into the C# Class Model
            NonAIWeatherResponse weatherData = JsonSerializer.Deserialize<NonAIWeatherResponse>(jsonResponse, options) ?? new NonAIWeatherResponse();

            // 4. Return deserialized weather data
            return weatherData;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return new NonAIWeatherResponse();
        }
    }
}
