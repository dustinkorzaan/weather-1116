using System.Text.Json.Serialization;

namespace Core.AIWeather.Models;

/// <summary>
/// Progressive update while the Foundry Agent processes an AI weather request.
/// </summary>
public class AIWeatherStreamUpdate
{
	[JsonPropertyName("type")]
	public required string Type { get; set; }

	[JsonPropertyName("message")]
	public string? Message { get; set; }

	[JsonPropertyName("delta")]
	public string? Delta { get; set; }

	[JsonPropertyName("result")]
	public AIWeatherResponse? Result { get; set; }
}
