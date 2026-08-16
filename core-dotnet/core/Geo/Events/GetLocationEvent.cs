using Core.Geo.Models;
using MediatR;

namespace Core.Geo.Events;

/// <summary>
/// Reverse-geocodes a latitude/longitude to a simple place label.
/// Prefers "City, State" (US) or "City, State, Country", then a feature name,
/// then a formatted coordinate such as "35.51° N, 86.58° W".
/// </summary>
public class GetLocationEvent : IRequest<NonAILocationResponse>
{
    public required double Latitude { get; set; }

    public required double Longitude { get; set; }
}
