using Core.geo.Models;
using Core.weather.Models;
using MediatR;

namespace Core.weather.Events;

/// <summary>
/// Fetches current weather for a latitude/longitude via the Open-Meteo forecast API.
/// </summary>
public class GetPublicWeatherDataEvent : IRequest<NonAIWeatherResponse>
{
    public required NonAILatLongResponse LatLong { get; set; }
}
