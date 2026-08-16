using System.Text.Json.Serialization;

namespace Core.Geo.Models;

internal class NominatimReverseResponse
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("address")]
    public NominatimAddress? Address { get; set; }
}
