using System.Text.Json.Serialization;

namespace Core.Weather.Models;

public class PublicWeatherHistoryResponse
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
    public PublicWeatherHistoryHourlyUnits? HourlyUnits { get; set; }

    [JsonPropertyName("hourly")]
    public PublicWeatherHistoryHourly? Hourly { get; set; }

    [JsonPropertyName("daily_units")]
    public PublicWeatherHistoryDailyUnits? DailyUnits { get; set; }

    [JsonPropertyName("daily")]
    public PublicWeatherHistoryDaily? Daily { get; set; }
}

public class PublicWeatherHistoryHourlyUnits
{
    [JsonPropertyName("time")]
    public string Time { get; set; } = string.Empty;

    [JsonPropertyName("temperature_2m")]
    public string Temperature2m { get; set; } = string.Empty;

    [JsonPropertyName("precipitation")]
    public string Precipitation { get; set; } = string.Empty;

    [JsonPropertyName("weather_code")]
    public string WeatherCode { get; set; } = string.Empty;

    [JsonPropertyName("wind_speed_10m")]
    public string WindSpeed10m { get; set; } = string.Empty;

    [JsonPropertyName("wind_direction_10m")]
    public string WindDirectionSource10m { get; set; } = string.Empty;
}

public class PublicWeatherHistoryHourly
{
    [JsonPropertyName("time")]
    public List<string> Time { get; set; } = [];

    [JsonPropertyName("temperature_2m")]
    public List<double> Temperature2m { get; set; } = [];

    [JsonPropertyName("precipitation")]
    public List<double> Precipitation { get; set; } = [];

    [JsonPropertyName("weather_code")]
    public List<int> WeatherCode { get; set; } = [];

    [JsonPropertyName("wind_speed_10m")]
    public List<double> WindSpeed10m { get; set; } = [];

    [JsonPropertyName("wind_direction_10m")]
    public List<int> WindDirectionSource10m { get; set; } = [];
}

public class PublicWeatherHistoryDailyUnits
{
    [JsonPropertyName("time")]
    public string Time { get; set; } = string.Empty;

    [JsonPropertyName("weather_code")]
    public string WeatherCode { get; set; } = string.Empty;

    [JsonPropertyName("temperature_2m_max")]
    public string Temperature2mMax { get; set; } = string.Empty;

    [JsonPropertyName("temperature_2m_min")]
    public string Temperature2mMin { get; set; } = string.Empty;

    [JsonPropertyName("precipitation_sum")]
    public string PrecipitationSum { get; set; } = string.Empty;

    [JsonPropertyName("wind_speed_10m_max")]
    public string WindSpeed10mMax { get; set; } = string.Empty;

    [JsonPropertyName("wind_direction_10m_dominant")]
    public string WindDirectionSource10mDominant { get; set; } = string.Empty;
}

public class PublicWeatherHistoryDaily
{
    [JsonPropertyName("time")]
    public List<string> Time { get; set; } = [];

    [JsonPropertyName("weather_code")]
    public List<int> WeatherCode { get; set; } = [];

    [JsonPropertyName("temperature_2m_max")]
    public List<double> Temperature2mMax { get; set; } = [];

    [JsonPropertyName("temperature_2m_min")]
    public List<double> Temperature2mMin { get; set; } = [];

    [JsonPropertyName("precipitation_sum")]
    public List<double> PrecipitationSum { get; set; } = [];

    [JsonPropertyName("wind_speed_10m_max")]
    public List<double> WindSpeed10mMax { get; set; } = [];

    [JsonPropertyName("wind_direction_10m_dominant")]
    public List<int> WindDirectionSource10mDominant { get; set; } = [];
}
