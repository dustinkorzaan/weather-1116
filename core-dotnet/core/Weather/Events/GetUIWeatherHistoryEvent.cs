using Core.Weather.Models;
using CQMediator;

namespace Core.Weather.Events;

/// <summary>
/// Fetches recent past weather for a latitude/longitude, converted to US customary units
/// (°F, mph, in) for direct use by the UI.
/// </summary>
public class GetUIWeatherHistoryEvent : IRequest<UIWeatherHistoryResponse>
{
    public required double Latitude { get; set; }

    public required double Longitude { get; set; }

    public PublicWeatherHistoryResolution Resolution { get; set; } = PublicWeatherHistoryResolution.Daily;
}
