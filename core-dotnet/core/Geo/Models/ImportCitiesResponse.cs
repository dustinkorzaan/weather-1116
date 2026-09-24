using System.Text.Json.Serialization;

namespace Core.Geo.Models;

public class ImportCitiesResponse
{
    /// <summary>Cities parsed from the downloaded cities500 export.</summary>
    [JsonPropertyName("downloaded")]
    public int Downloaded { get; set; }

    /// <summary>ImportCitiesUpsertEvent jobs enqueued, one per batch of cities.</summary>
    [JsonPropertyName("enqueued")]
    public int Enqueued { get; set; }

    [JsonPropertyName("deleted")]
    public int Deleted { get; set; }
}
