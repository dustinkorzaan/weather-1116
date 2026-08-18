using System.Text.Json.Serialization;

namespace Core.Weather.Models;

/// <summary>
/// UI-facing weather history, mapped from <see cref="PublicWeatherHistoryResponse"/> into
/// US customary units (°F, mph, in) so the UI only needs to format values, not convert them.
/// </summary>
public class UIWeatherHistoryResponse
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("timezone")]
    public string Timezone { get; set; } = string.Empty;

    [JsonPropertyName("hourly")]
    public UIWeatherHourlySeries? Hourly { get; set; }

    [JsonPropertyName("daily")]
    public UIWeatherDailySeries? Daily { get; set; }
}
