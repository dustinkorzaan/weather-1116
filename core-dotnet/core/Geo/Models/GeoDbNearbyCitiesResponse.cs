using System.Text.Json.Serialization;

namespace Core.Geo.Models;

/// <summary>GeoDB Cities <c>/v1/geo/locations/{locationId}/nearbyCities</c> response.</summary>
public class GeoDbNearbyCitiesResponse
{
    [JsonPropertyName("data")]
    public List<GeoDbCity>? Data { get; set; }

    [JsonPropertyName("metadata")]
    public GeoDbMetadata? Metadata { get; set; }
}

public class GeoDbCity
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("region")]
    public string? Region { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("distance")]
    public double? Distance { get; set; }

    [JsonPropertyName("population")]
    public long? Population { get; set; }
}

public class GeoDbMetadata
{
    [JsonPropertyName("currentOffset")]
    public int CurrentOffset { get; set; }

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
}
