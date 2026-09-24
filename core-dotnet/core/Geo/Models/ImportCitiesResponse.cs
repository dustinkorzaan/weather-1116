using System.Text.Json.Serialization;

namespace Core.Geo.Models;

public class ImportCitiesResponse
{
    /// <summary>Unique cities parsed from the downloaded cities500 export.</summary>
    [JsonPropertyName("downloaded")]
    public int Downloaded { get; set; }

    /// <summary>ImportCitiesUpsertEvent jobs enqueued, one per batch file of cities.</summary>
    [JsonPropertyName("enqueued")]
    public int Enqueued { get; set; }
}
