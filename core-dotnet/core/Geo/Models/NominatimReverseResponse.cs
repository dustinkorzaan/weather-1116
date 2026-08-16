using System.Text.Json.Serialization;

namespace Core.Geo.Models;

public class NominatimReverseResponse
{
    [JsonPropertyName("address")]
    public NominatimAddress? Address { get; set; }
}
