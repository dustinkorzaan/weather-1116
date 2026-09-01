using Core.AIWeather.Models;
using CQMediator;

namespace Core.AIWeather.Events;

/// <summary>
/// Probes current AI weather for Nashville, TN and confirms a valid response.
/// </summary>
public class ConfirmNashvilleAIWeatherEvent : IRequest<AIWeatherResponse>
{
    /// <summary>Which GetCurrentAIWeather handler version to probe (3 or 4). Defaults to 3.</summary>
    public int Version { get; set; } = 3;
}
