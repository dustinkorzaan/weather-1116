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
}
