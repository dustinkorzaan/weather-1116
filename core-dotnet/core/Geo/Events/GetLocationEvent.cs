using Core.Geo.Models;
using MediatR;

namespace Core.Geo.Events;

/// <summary>
/// Reverse-geocodes a latitude/longitude to a simple place label.
/// US results are "City, State"; elsewhere "City, State, Country".
/// </summary>
public class GetLocationEvent : IRequest<NonAILocationResponse>
{
    public required double Latitude { get; set; }

    public required double Longitude { get; set; }
}
