using System.Text.Json.Serialization;

namespace Core.weather.Models;

public class NonAIWeatherResponse
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("generationtime_ms")]
    public double GenerationTimeMs { get; set; }

    [JsonPropertyName("utc_offset_seconds")]
    public int UtcOffsetSeconds { get; set; }

    [JsonPropertyName("timezone")]
    public string Timezone { get; set; } = string.Empty;

    [JsonPropertyName("timezone_abbreviation")]
    public string TimezoneAbbreviation { get; set; } = string.Empty;

    [JsonPropertyName("elevation")]
    public double Elevation { get; set; }

    [JsonPropertyName("current_weather_units")]
    public NonAICurrentWeatherUnits CurrentWeatherUnits { get; set; } = new();

    [JsonPropertyName("current_weather")]
    public NonAICurrentWeather CurrentWeather { get; set; } = new();
}
