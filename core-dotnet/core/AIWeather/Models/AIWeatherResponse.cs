using System.Text.Json.Serialization;

namespace Core.AIWeather.Models;

public class AIWeatherResponse
{
    [JsonPropertyName("fullSummary")]
    public string FullSummary { get; set; } = string.Empty;

    [JsonPropertyName("temperatureF")]
    public double TemperatureF { get; set; }

    [JsonPropertyName("windSpeedMPH")]
    public double WindSpeedMPH { get; set; }

    [JsonPropertyName("windDirection")]
    public string WindDirection { get; set; } = string.Empty;

    [JsonPropertyName("windDirectionFromDegrees")]
    public int WindDirectionFromDegrees { get; set; }

    [JsonPropertyName("conditions")]
    public string Conditions { get; set; } = string.Empty;

    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }
}
