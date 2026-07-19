using System.Text.Json.Serialization;

namespace Core.geo.Models;

public class NonAILatLongResponse
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }
}
