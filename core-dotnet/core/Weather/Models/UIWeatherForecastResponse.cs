using System.Text.Json.Serialization;

namespace Core.Weather.Models;

/// <summary>
/// UI-facing weather forecast, mapped from <see cref="PublicWeatherForecastResponse"/> into
/// US customary units (°F, mph, in) so the UI only needs to format values, not convert them.
/// </summary>
public class UIWeatherForecastResponse
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

    [JsonPropertyName("minutely15")]
    public UIWeatherHourlySeries? Minutely15 { get; set; }
}
