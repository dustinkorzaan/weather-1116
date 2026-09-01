using Core.Weather.Models;
using CQMediator;

namespace Core.Weather.Events;

/// <summary>
/// Fetches an upcoming weather forecast for a latitude/longitude, converted to US customary
/// units (°F, mph, in) for direct use by the UI.
/// </summary>
public class GetUIWeatherForecastEvent : IRequest<UIWeatherForecastResponse>
{
    public required double Latitude { get; set; }

    public required double Longitude { get; set; }

    public PublicWeatherForecastResolution Resolution { get; set; } = PublicWeatherForecastResolution.Daily;
}
