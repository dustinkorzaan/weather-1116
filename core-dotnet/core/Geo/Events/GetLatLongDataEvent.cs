using Core.Geo.Models;
using MediatR;

namespace Core.Geo.Events;

/// <summary>
/// Resolves a location name to latitude/longitude via the Open-Meteo geocoding API.
/// </summary>
public class GetLatLongDataEvent : IRequest<NonAILatLongResponse>
{
    public required string Location { get; set; }
}
