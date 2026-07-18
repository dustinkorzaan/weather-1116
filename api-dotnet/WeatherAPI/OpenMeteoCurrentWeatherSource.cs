using Core.currentweather;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace WeatherAPI;

/// <summary>
/// Fetches current weather conditions from the free Open Meteo API
/// (https://open-meteo.com). No API key is required.
/// </summary>
public class OpenMeteoCurrentWeatherSource : ICurrentWeatherSource
{
    private readonly HttpClient _httpClient;

    public OpenMeteoCurrentWeatherSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CurrentWeatherConditions> GetCurrentWeatherAsync(string location, CancellationToken cancellationToken)
    {
        var (latitude, longitude) = await GetCoordinatesAsync(location, cancellationToken);

        var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current_weather=true";
        var weather = await _httpClient.GetFromJsonAsync<OpenMeteoWeatherResponse>(weatherUrl, cancellationToken)
            ?? throw new InvalidOperationException("Empty weather response from Open Meteo.");

        var cw = weather.CurrentWeather;

        return new CurrentWeatherConditions
        {
            Location = location,
            Latitude = latitude,
            Longitude = longitude,
            TemperatureC = cw.Temperature,
            WindSpeedKph = cw.WindSpeed,
            WindDirectionDeg = cw.WindDirection,
            IsDay = cw.IsDay == 1,
            WeatherCode = cw.WeatherCode,
            ObservedAt = cw.Time,
        };
    }

    private async Task<(double Latitude, double Longitude)> GetCoordinatesAsync(string location, CancellationToken cancellationToken)
    {
        var queries = new List<string> { location };
        if (location.Contains(','))
        {
            queries.Add(location.Split(',')[0].Trim());
        }

        foreach (var query in queries.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(query)}&count=1&language=en&format=json";
            var geoData = await _httpClient.GetFromJsonAsync<GeocodingResponse>(url, cancellationToken);

            if (geoData?.Results is { Count: > 0 })
            {
                var top = geoData.Results[0];
                return (top.Latitude, top.Longitude);
            }
        }

        throw new InvalidOperationException($"Could not resolve coordinates for location: {location}");
    }

    private sealed class GeocodingResponse
    {
        [JsonPropertyName("results")]
        public List<GeocodingResult> Results { get; set; } = [];
    }

    private sealed class GeocodingResult
    {
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }
    }

    private sealed class OpenMeteoWeatherResponse
    {
        [JsonPropertyName("current_weather")]
        public CurrentWeatherData CurrentWeather { get; set; } = new();
    }

    private sealed class CurrentWeatherData
    {
        [JsonPropertyName("time")]
        public string Time { get; set; } = string.Empty;

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }

        [JsonPropertyName("windspeed")]
        public double WindSpeed { get; set; }

        [JsonPropertyName("winddirection")]
        public int WindDirection { get; set; }

        [JsonPropertyName("is_day")]
        public int IsDay { get; set; }

        [JsonPropertyName("weathercode")]
        public int WeatherCode { get; set; }
    }
}
