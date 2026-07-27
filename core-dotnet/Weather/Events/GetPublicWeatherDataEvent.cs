using Core.Geo.Models;
using Core.Weather.Models;
using MediatR;

namespace Core.Weather.Events;

/// <summary>
/// Fetches current weather for a latitude/longitude via the Open-Meteo forecast API.
/// </summary>
public class GetPublicWeatherDataEvent : IRequest<NonAIWeatherResponse>
{
    public required NonAILatLongResponse LatLong { get; set; }
}
