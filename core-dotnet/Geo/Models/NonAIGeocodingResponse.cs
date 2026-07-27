using System.Text.Json.Serialization;

namespace Core.Geo.Models;

public class NonAIGeocodingResponse
{
    [JsonPropertyName("results")]
    public List<NonAIGeocodingResult> Results { get; set; } = [];
}
