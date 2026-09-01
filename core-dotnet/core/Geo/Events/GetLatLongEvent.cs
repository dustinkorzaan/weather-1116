using Core.Geo.Models;
using CQMediator;

namespace Core.Geo.Events;

/// <summary>
/// Resolves a location name to ranked latitude/longitude matches via the Open-Meteo geocoding API.
/// Rank 1 is the best match. <see cref="Count"/> defaults to 5 (max 100).
/// </summary>
public class GetLatLongEvent : IRequest<NonAILatLongListResponse>
{
    public const int DefaultCount = 5;
    public const int MaxCount = 100;

    public required string Location { get; set; }

    public int Count { get; set; } = DefaultCount;
}
