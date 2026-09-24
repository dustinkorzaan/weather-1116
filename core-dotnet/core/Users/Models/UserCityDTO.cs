using System.Text.Json.Serialization;

namespace Core.Users.Models;

public class UserCityDTO
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("locationName")]
    public string LocationName { get; set; } = string.Empty;
}
