using System.Text.Json.Serialization;

namespace Core.Geo.Models;

public class ImportCitiesResponse
{
    /// <summary>Cities parsed from the downloaded cities500 export.</summary>
    [JsonPropertyName("downloaded")]
    public int Downloaded { get; set; }

    [JsonPropertyName("inserted")]
    public int Inserted { get; set; }

    [JsonPropertyName("updated")]
    public int Updated { get; set; }

    [JsonPropertyName("deleted")]
    public int Deleted { get; set; }

    [JsonPropertyName("unchanged")]
    public int Unchanged { get; set; }
}
