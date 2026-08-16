using System.Globalization;
using System.Text.Json;
using Core.Weather.Events;
using Core.Weather.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Core.Weather.Handlers;

/// <summary>
/// Fetches public current-weather data from Open-Meteo for a given lat/long.
/// </summary>
public class GetPublicWeatherCurrentHandler : IRequestHandler<GetPublicWeatherCurrentEvent, NonAIWeatherResponse>
{
    private readonly ILogger<GetPublicWeatherCurrentHandler> _logger;

    public GetPublicWeatherCurrentHandler(ILogger<GetPublicWeatherCurrentHandler> logger)
    {
        _logger = logger;
    }

    public async Task<NonAIWeatherResponse> Handle(GetPublicWeatherCurrentEvent request, CancellationToken cancellationToken)
    {
        var client = new HttpClient();
        string endpoint = BuildCurrentWeatherUrl(request.Latitude, request.Longitude);

        string jsonResponse = await client.GetStringAsync(endpoint, cancellationToken);

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
