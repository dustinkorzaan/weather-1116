using System.Text.Json.Serialization;

namespace Core.Geo.Models;

/// <summary>
/// Ranked geocoding matches for a location query (rank 1 is the best match).
/// </summary>
public class NonAILatLongListResponse
{
    [JsonPropertyName("results")]
    public List<NonAILatLongResponse> Results { get; set; } = [];
}
