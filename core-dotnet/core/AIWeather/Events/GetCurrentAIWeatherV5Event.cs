using Core.AIWeather.Models;
using MediatR;

namespace Core.AIWeather.Events;

/// <summary>
/// Asks a hosted Microsoft Foundry Agent for current weather at a location.
/// </summary>
public class GetCurrentAIWeatherV5Event : IRequest<AIWeatherResponse>
{
    public required string Location { get; set; }
}
