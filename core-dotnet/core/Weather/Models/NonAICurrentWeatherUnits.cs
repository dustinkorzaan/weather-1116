using System.Text.Json.Serialization;

namespace Core.Weather.Models;

/// <summary>Open-Meteo <c>current_weather_units</c> block.</summary>
public class NonAICurrentWeatherUnits
{
    [JsonPropertyName("time")]
    public string Time { get; set; } = string.Empty;

    [JsonPropertyName("interval")]
    public string Interval { get; set; } = string.Empty;

    [JsonPropertyName("temperature")]
    public string TemperatureC { get; set; } = string.Empty;

    [JsonPropertyName("windspeed")]
    public string WindSpeedKmh { get; set; } = string.Empty;

    [JsonPropertyName("winddirection")]
    public string WindDirectionSourceDegrees { get; set; } = string.Empty;

    [JsonPropertyName("is_day")]
    public string IsDay { get; set; } = string.Empty;

    [JsonPropertyName("weathercode")]
    public string WeatherCode { get; set; } = string.Empty;
}
