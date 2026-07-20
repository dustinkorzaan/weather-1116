using System.Text.Json.Serialization;

namespace Core.AIWeather.Models;

public class AIWeatherResponse
{
	[JsonPropertyName("summary")]
	public string Summary { get; set; } = string.Empty;

	[JsonPropertyName("temperature")]
	public double Temperature { get; set; }

	[JsonPropertyName("windSpeed")]
	public double WindSpeed { get; set; }

	[JsonPropertyName("windDirection")]
	public string WindDirection { get; set; } = string.Empty;

	[JsonPropertyName("conditions")]
	public string Conditions { get; set; } = string.Empty;
}
