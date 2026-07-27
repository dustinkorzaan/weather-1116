using System.Text.Json.Serialization;

namespace Core.Weather.Models;

public class NonAICurrentWeather
{
    [JsonPropertyName("time")]
    public string Time { get; set; } = string.Empty;

    [JsonPropertyName("interval")]
    public int Interval { get; set; }

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
