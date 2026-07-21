using Core.AIWeather.Models;
using MediatR;

namespace Core.AIWeather.Events;

/// <summary>
/// Streams progressive updates while the hosted Microsoft Foundry Agent processes a weather request.
/// </summary>
public class GetCurrentAIWeatherStreamEvent : IStreamRequest<AIWeatherStreamUpdate>
{
	public required string Location { get; set; }
}
