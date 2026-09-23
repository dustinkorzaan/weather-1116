using System.Text.Json.Serialization;

namespace Core.Geo.Models;

public class NonAICitiesResponse
{
    /// <summary>Search radius actually used, after resetting into range.</summary>
    [JsonPropertyName("distanceKm")]
    public double DistanceKm { get; set; }

    /// <summary>Maximum number of cities requested, after resetting into range.</summary>
    [JsonPropertyName("size")]
    public int Size { get; set; }

    [JsonPropertyName("returned")]
    public int Returned { get; set; }

    /// <summary>Cities GeoDB reports within the radius, which can exceed <see cref="Returned"/>.</summary>
    [JsonPropertyName("totalAvailable")]
    public int TotalAvailable { get; set; }

    /// <summary>Largest population first.</summary>
    [JsonPropertyName("cities")]
    public List<NonAICity> Cities { get; set; } = [];
}
