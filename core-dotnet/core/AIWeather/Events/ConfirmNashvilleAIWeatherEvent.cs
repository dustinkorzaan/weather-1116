using Core.AIWeather.Models;
using MediatR;

namespace Core.AIWeather.Events;

/// <summary>
/// Probes current AI weather for Nashville, TN and confirms a valid response.
/// </summary>
public class ConfirmNashvilleAIWeatherEvent : IRequest<AIWeatherResponse>
{
}
