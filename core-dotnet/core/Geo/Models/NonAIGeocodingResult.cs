using System.Text.Json.Serialization;

namespace Core.Geo.Models;

public class NonAIGeocodingResult
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("admin1")]
    public string Admin1 { get; set; } = string.Empty;

    [JsonPropertyName("country")]
    public string Country { get; set; } = string.Empty;

    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }
}
