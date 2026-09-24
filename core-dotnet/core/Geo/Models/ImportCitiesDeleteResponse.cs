using System.Text.Json.Serialization;

namespace Core.Geo.Models;

public class ImportCitiesDeleteResponse
{
    [JsonPropertyName("deleted")]
    public int Deleted { get; set; }
}
