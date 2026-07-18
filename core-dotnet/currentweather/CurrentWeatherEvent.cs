using MediatR;

namespace Core.currentweather;

/// <summary>
/// Requests the current weather conditions for a location string (e.g. "Nashville, TN").
/// </summary>
public class CurrentWeatherEvent : IRequest<CurrentWeatherConditions>
{
    public required string Location { get; set; }
}
