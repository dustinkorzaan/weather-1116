using System.Text.Json.Serialization;

namespace Core.Weather.Models;

/// <summary>Open-Meteo <c>current_weather</c> block (metric units requested explicitly).</summary>
public class NonAICurrentWeather
{
    [JsonPropertyName("time")]
    public string Time { get; set; } = string.Empty;

    [JsonPropertyName("interval")]
    public int Interval { get; set; }

    [JsonPropertyName("temperature")]
    public double TemperatureC { get; set; }

    [JsonPropertyName("windspeed")]
    public double WindSpeedKmh { get; set; }

    [JsonPropertyName("winddirection")]
    public int WindDirectionSourceDegrees { get; set; }

    [JsonPropertyName("is_day")]
    public int IsDay { get; set; }

    [JsonPropertyName("weathercode")]
    public int WeatherCode { get; set; }
}
