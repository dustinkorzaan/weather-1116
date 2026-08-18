using System.Text.Json.Serialization;

using Core.Weather;

namespace Core.AIWeather.Models;

/// <summary>
/// Strict JSON schema the hosted model fills. Wind direction uses meteorological
/// from-degrees copied from the weather tool; map to <see cref="AIWeatherResponse"/>
/// before returning to clients.
/// </summary>
public class AIWeatherModelResponse
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

    public AIWeatherResponse ToApiResponse()
    {
        var towardsDegrees = WeatherUnitConversion.MeteorologicalFromToWindTowards(WindDirectionFromDegrees);
        return new AIWeatherResponse
        {
            FullSummary = FullSummary,
            TemperatureF = TemperatureF,
            WindSpeedMPH = WindSpeedMPH,
            WindDirection = WeatherUnitConversion.DegreesToCompass(towardsDegrees),
            WindDirectionTowardsDegrees = towardsDegrees,
            Conditions = Conditions,
            Latitude = Latitude,
            Longitude = Longitude,
        };
    }
}
