using Core.AIWeather.Models;
using CQMediator;

namespace Core.AIWeather.Events;

/// <summary>
/// Asks the hosted Microsoft Foundry Agent for current weather at a location.
/// </summary>
public class GetCurrentAIWeatherV3Event : IRequest<AIWeatherResponse>
{
    public required string Location { get; set; }
}
