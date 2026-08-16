using System.Text.Json.Serialization;

namespace Core.Geo.Models;

public class NominatimLocationResponse
{
    [JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;
}
