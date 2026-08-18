using System.Text.Json.Serialization;
using Core.Weather;

namespace Core.AIWeather.Models;

/// <summary>Strict model JSON output; <see cref="AIWeatherResponse.WindDirectionSource"/> is computed server-side from degrees.</summary>
public class AIWeatherModelResponse
{
    [JsonPropertyName("fullSummary")]
    public string FullSummary { get; set; } = string.Empty;

    [JsonPropertyName("temperatureF")]
    public double TemperatureF { get; set; }

    [JsonPropertyName("windSpeedMPH")]
    public double WindSpeedMPH { get; set; }

    [JsonPropertyName("windDirectionSourceDegrees")]
    public int WindDirectionSourceDegrees { get; set; }

    [JsonPropertyName("conditions")]
    public string Conditions { get; set; } = string.Empty;

    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    public AIWeatherResponse ToApiResponse()
    {
        var windDirectionSourceDegrees =
            WeatherUnitConversion.NormalizeSourceDegrees(WindDirectionSourceDegrees);

        return new AIWeatherResponse
        {
            FullSummary = FullSummary,
            TemperatureF = TemperatureF,
            WindSpeedMPH = WindSpeedMPH,
            WindDirectionSourceDegrees = windDirectionSourceDegrees,
            WindDirectionSource = WeatherUnitConversion.DegreesToCompass(windDirectionSourceDegrees),
            Conditions = Conditions,
            Latitude = Latitude,
            Longitude = Longitude,
        };
    }
}
