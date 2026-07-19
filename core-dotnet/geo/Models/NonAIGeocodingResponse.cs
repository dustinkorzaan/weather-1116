using System.Text.Json.Serialization;

namespace Core.geo.Models;

public class NonAIGeocodingResponse
{
    [JsonPropertyName("results")]
    public List<NonAIGeocodingResult> Results { get; set; } = [];
}
