using System.Text.Json.Serialization;

namespace Core.Weather.Models;

public class PublicWeatherForecastResponse
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
    public PublicWeatherForecastHourlyUnits? HourlyUnits { get; set; }

    [JsonPropertyName("hourly")]
    public PublicWeatherForecastHourly? Hourly { get; set; }

    [JsonPropertyName("daily_units")]
    public PublicWeatherForecastDailyUnits? DailyUnits { get; set; }

    [JsonPropertyName("daily")]
    public PublicWeatherForecastDaily? Daily { get; set; }

    [JsonPropertyName("minutely_15_units")]
    public PublicWeatherForecastMinutely15Units? Minutely15Units { get; set; }

    [JsonPropertyName("minutely_15")]
    public PublicWeatherForecastMinutely15? Minutely15 { get; set; }
}

public class PublicWeatherForecastHourlyUnits
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
    public string WindDirection10m { get; set; } = string.Empty;
}

public class PublicWeatherForecastHourly
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
    public List<int> WindDirection10m { get; set; } = [];
}

public class PublicWeatherForecastDailyUnits
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
    public string WindDirection10mDominant { get; set; } = string.Empty;
}

public class PublicWeatherForecastDaily
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
    public List<int> WindDirection10mDominant { get; set; } = [];
}

public class PublicWeatherForecastMinutely15Units
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
    public string WindDirection10m { get; set; } = string.Empty;
}

public class PublicWeatherForecastMinutely15
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
    public List<int> WindDirection10m { get; set; } = [];
}
