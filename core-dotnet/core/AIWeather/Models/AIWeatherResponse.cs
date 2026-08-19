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

    [JsonPropertyName("windDirectionSource")]
    public string WindDirectionSource { get; set; } = string.Empty;

    [JsonPropertyName("windDirectionSourceDegrees")]
    public int WindDirectionSourceDegrees { get; set; }

    [JsonPropertyName("conditions")]
    public string Conditions { get; set; } = string.Empty;

    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("runLogDetails")]
    public List<RunLogDetail> RunLogDetails { get; set; } = [];
}
