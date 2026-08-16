using System.Text.Json;
using Core.Weather.Models;

namespace Core.Tests.Weather.Models;

/// <summary>
/// Verifies the JSON contracts the Core weather handlers rely on when deserializing
/// Open-Meteo forecast responses.
/// </summary>
public class WeatherModelsTests
{
    [Fact]
    public void NonAIWeatherResponse_DeserializesOpenMeteoForecastShape()
    {
        const string json = """
        {
          "latitude": 36.16,
          "longitude": -86.78,
          "generationtime_ms": 0.12,
          "utc_offset_seconds": -21600,
          "timezone": "America/Chicago",
          "timezone_abbreviation": "CST",
          "elevation": 145.0,
          "current_weather_units": {
            "time": "iso8601",
            "interval": "seconds",
            "temperature": "°F",
            "windspeed": "mp/h",
            "winddirection": "°",
            "is_day": "",
            "weathercode": "wmo code"
          },
          "current_weather": {
            "time": "2024-01-02T03:00",
            "interval": 900,
            "temperature": 41.2,
            "windspeed": 7.5,
            "winddirection": 200,
            "is_day": 1,
            "weathercode": 3
          }
        }
        """;

        var result = JsonSerializer.Deserialize<NonAIWeatherResponse>(json);

        Assert.NotNull(result);
        Assert.Equal(36.16, result!.Latitude);
        Assert.Equal(-86.78, result.Longitude);
        Assert.Equal(-21600, result.UtcOffsetSeconds);
        Assert.Equal("America/Chicago", result.Timezone);
        Assert.Equal("CST", result.TimezoneAbbreviation);

        Assert.Equal("°F", result.CurrentWeatherUnits.Temperature);
        Assert.Equal("mp/h", result.CurrentWeatherUnits.WindSpeed);

        Assert.Equal("2024-01-02T03:00", result.CurrentWeather.Time);
        Assert.Equal(900, result.CurrentWeather.Interval);
        Assert.Equal(41.2, result.CurrentWeather.Temperature);
        Assert.Equal(7.5, result.CurrentWeather.WindSpeed);
        Assert.Equal(200, result.CurrentWeather.WindDirection);
        Assert.Equal(1, result.CurrentWeather.IsDay);
        Assert.Equal(3, result.CurrentWeather.WeatherCode);
    }

    [Fact]
    public void NonAIWeatherResponse_MissingNestedObjects_UsesNonNullDefaults()
    {
        var result = JsonSerializer.Deserialize<NonAIWeatherResponse>("{}");

        Assert.NotNull(result);
        Assert.NotNull(result!.CurrentWeather);
        Assert.NotNull(result.CurrentWeatherUnits);
        Assert.Equal(string.Empty, result.Timezone);
    }

    [Fact]
    public void PublicWeatherForecastResponse_DeserializesOpenMeteoDailyShape()
    {
        const string json = """
        {
          "latitude": 36.16,
          "longitude": -86.78,
          "generationtime_ms": 0.12,
          "utc_offset_seconds": -21600,
          "timezone": "America/Chicago",
          "timezone_abbreviation": "CST",
          "elevation": 145.0,
          "daily_units": {
            "time": "iso8601",
            "weather_code": "wmo code",
            "temperature_2m_max": "°F",
            "temperature_2m_min": "°F",
            "precipitation_sum": "mm",
            "wind_speed_10m_max": "mp/h",
            "wind_direction_10m_dominant": "°"
          },
          "daily": {
            "time": ["2026-08-16", "2026-08-17"],
            "weather_code": [2, 3],
            "temperature_2m_max": [100.4, 97.9],
            "temperature_2m_min": [73.9, 79.4],
            "precipitation_sum": [0.00, 0.40],
            "wind_speed_10m_max": [8.8, 14.0],
            "wind_direction_10m_dominant": [232, 298]
          }
        }
        """;

        var result = JsonSerializer.Deserialize<PublicWeatherForecastResponse>(json);

        Assert.NotNull(result);
        Assert.Equal("America/Chicago", result!.Timezone);
        Assert.Null(result.Hourly);
        Assert.Null(result.Minutely15);
        Assert.NotNull(result.Daily);
        Assert.Equal(["2026-08-16", "2026-08-17"], result.Daily!.Time);
        Assert.Equal([100.4, 97.9], result.Daily.Temperature2mMax);
        Assert.Equal("°F", result.DailyUnits!.Temperature2mMax);
    }

    [Fact]
    public void PublicWeatherHistoryResponse_DeserializesOpenMeteoHourlyShape()
    {
        const string json = """
        {
          "latitude": 36.16,
          "longitude": -86.78,
          "generationtime_ms": 0.12,
          "utc_offset_seconds": -21600,
          "timezone": "America/Chicago",
          "timezone_abbreviation": "CST",
          "elevation": 145.0,
          "hourly_units": {
            "time": "iso8601",
            "temperature_2m": "°F",
            "precipitation": "mm",
            "weather_code": "wmo code",
            "wind_speed_10m": "mp/h",
            "wind_direction_10m": "°"
          },
          "hourly": {
            "time": ["2026-08-16T00:00", "2026-08-16T01:00"],
            "temperature_2m": [72.1, 71.5],
            "precipitation": [0.00, 0.10],
            "weather_code": [1, 2],
            "wind_speed_10m": [5.5, 6.2],
            "wind_direction_10m": [180, 190]
          }
        }
        """;

        var result = JsonSerializer.Deserialize<PublicWeatherHistoryResponse>(json);

        Assert.NotNull(result);
        Assert.Null(result!.Daily);
        Assert.NotNull(result.Hourly);
        Assert.Equal(["2026-08-16T00:00", "2026-08-16T01:00"], result.Hourly!.Time);
        Assert.Equal([72.1, 71.5], result.Hourly.Temperature2m);
        Assert.Equal([180, 190], result.Hourly.WindDirection10m);
    }
}
