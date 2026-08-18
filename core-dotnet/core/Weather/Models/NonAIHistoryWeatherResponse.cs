using System.Text.Json.Serialization;

namespace Core.Weather.Models;

/// <summary>Open-Meteo forecast API response for past weather series.</summary>
public class NonAIHistoryWeatherResponse
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("generationtime_ms")]
    public double GenerationTimeMs { get; set; }

    [JsonPropertyName("utc_offset_seconds")]
    public int UtcOffsetSeconds { get; set; }

    [JsonPropertyName("timezone")]
    public string Timezone { get; set; } = string.Empty;

    [JsonPropertyName("timezone_abbreviation")]
    public string TimezoneAbbreviation { get; set; } = string.Empty;

    [JsonPropertyName("elevation")]
    public double Elevation { get; set; }

    [JsonPropertyName("hourly_units")]
    public NonAIHistoryWeatherHourlyUnits? HourlyUnits { get; set; }

    [JsonPropertyName("hourly")]
    public NonAIHistoryWeatherHourly? Hourly { get; set; }

    [JsonPropertyName("daily_units")]
    public NonAIHistoryWeatherDailyUnits? DailyUnits { get; set; }

    [JsonPropertyName("daily")]
    public NonAIHistoryWeatherDaily? Daily { get; set; }
}

public class NonAIHistoryWeatherHourlyUnits
{
    [JsonPropertyName("time")]
    public string Time { get; set; } = string.Empty;

    [JsonPropertyName("temperature_2m")]
    public string Temperature2mC { get; set; } = string.Empty;

    [JsonPropertyName("precipitation")]
    public string PrecipitationMm { get; set; } = string.Empty;

    [JsonPropertyName("weather_code")]
    public string WeatherCode { get; set; } = string.Empty;

    [JsonPropertyName("wind_speed_10m")]
    public string WindSpeed10mKmh { get; set; } = string.Empty;

    [JsonPropertyName("wind_direction_10m")]
    public string WindDirectionSource10m { get; set; } = string.Empty;
}

public class NonAIHistoryWeatherHourly
{
    [JsonPropertyName("time")]
    public List<string> Time { get; set; } = [];

    [JsonPropertyName("temperature_2m")]
    public List<double> Temperature2mC { get; set; } = [];

    [JsonPropertyName("precipitation")]
    public List<double> PrecipitationMm { get; set; } = [];

    [JsonPropertyName("weather_code")]
    public List<int> WeatherCode { get; set; } = [];

    [JsonPropertyName("wind_speed_10m")]
    public List<double> WindSpeed10mKmh { get; set; } = [];

    [JsonPropertyName("wind_direction_10m")]
    public List<int> WindDirectionSource10m { get; set; } = [];
}

public class NonAIHistoryWeatherDailyUnits
{
    [JsonPropertyName("time")]
    public string Time { get; set; } = string.Empty;

    [JsonPropertyName("weather_code")]
    public string WeatherCode { get; set; } = string.Empty;

    [JsonPropertyName("temperature_2m_max")]
    public string Temperature2mMaxC { get; set; } = string.Empty;

    [JsonPropertyName("temperature_2m_min")]
    public string Temperature2mMinC { get; set; } = string.Empty;

    [JsonPropertyName("precipitation_sum")]
    public string PrecipitationSumMm { get; set; } = string.Empty;

    [JsonPropertyName("wind_speed_10m_max")]
    public string WindSpeed10mMaxKmh { get; set; } = string.Empty;

    [JsonPropertyName("wind_direction_10m_dominant")]
    public string WindDirectionSource10mDominant { get; set; } = string.Empty;
}

public class NonAIHistoryWeatherDaily
{
    [JsonPropertyName("time")]
    public List<string> Time { get; set; } = [];

    [JsonPropertyName("weather_code")]
    public List<int> WeatherCode { get; set; } = [];

    [JsonPropertyName("temperature_2m_max")]
    public List<double> Temperature2mMaxC { get; set; } = [];

    [JsonPropertyName("temperature_2m_min")]
    public List<double> Temperature2mMinC { get; set; } = [];

    [JsonPropertyName("precipitation_sum")]
    public List<double> PrecipitationSumMm { get; set; } = [];

    [JsonPropertyName("wind_speed_10m_max")]
    public List<double> WindSpeed10mMaxKmh { get; set; } = [];

    [JsonPropertyName("wind_direction_10m_dominant")]
    public List<int> WindDirectionSource10mDominant { get; set; } = [];
}
