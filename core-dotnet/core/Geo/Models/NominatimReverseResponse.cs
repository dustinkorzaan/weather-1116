using System.Text.Json.Serialization;

namespace Core.Geo.Models;

internal class NominatimReverseResponse
{
    [JsonPropertyName("address")]
    public NominatimAddress? Address { get; set; }
}
