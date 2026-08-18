using System.Text.Json.Serialization;

namespace Core.Weather.Models;

/// <summary>UI-facing hourly/15-minute weather series, in US customary units.</summary>
public class UIWeatherHourlySeries
{
    [JsonPropertyName("time")]
    public List<string> Time { get; set; } = [];

    [JsonPropertyName("temperatureF")]
    public List<double> TemperatureF { get; set; } = [];

    [JsonPropertyName("precipitationInch")]
    public List<double> PrecipitationInch { get; set; } = [];

    [JsonPropertyName("weatherCode")]
    public List<int> WeatherCode { get; set; } = [];

    [JsonPropertyName("windSpeedMPH")]
    public List<double> WindSpeedMPH { get; set; } = [];

    [JsonPropertyName("windDirectionFromDegrees")]
    public List<int> WindDirectionFromDegrees { get; set; } = [];
}

/// <summary>UI-facing daily weather series, in US customary units.</summary>
public class UIWeatherDailySeries
{
    [JsonPropertyName("time")]
    public List<string> Time { get; set; } = [];

    [JsonPropertyName("weatherCode")]
    public List<int> WeatherCode { get; set; } = [];

    [JsonPropertyName("temperatureHighF")]
    public List<double> TemperatureHighF { get; set; } = [];

    [JsonPropertyName("temperatureLowF")]
    public List<double> TemperatureLowF { get; set; } = [];

    [JsonPropertyName("precipitationInch")]
    public List<double> PrecipitationInch { get; set; } = [];

    [JsonPropertyName("windSpeedMPH")]
    public List<double> WindSpeedMPH { get; set; } = [];

    [JsonPropertyName("windDirectionFromDegrees")]
    public List<int> WindDirectionFromDegrees { get; set; } = [];
}
