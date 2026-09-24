using System.Text.Json.Serialization;

namespace Core.Geo.Models;

public class ImportCitiesUpsertResponse
{
    [JsonPropertyName("inserted")]
    public int Inserted { get; set; }

    [JsonPropertyName("updated")]
    public int Updated { get; set; }

    [JsonPropertyName("unchanged")]
    public int Unchanged { get; set; }
}
