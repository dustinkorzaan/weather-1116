using System.Text.Json.Serialization;

namespace Core.Geo.Models;

public class NonAICity
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("region")]
    public string? Region { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("distanceKm")]
    public double DistanceKm { get; set; }

    [JsonPropertyName("population")]
    public long? Population { get; set; }
}
