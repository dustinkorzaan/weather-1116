using System.Text.Json;
using Core.AIWeather.Models;
using Core.geo.Models;
using Core.weather.Models;

namespace Core.Tests.Models;

/// <summary>
/// Verifies the JSON contracts the Core handlers rely on when deserializing
/// Open-Meteo / Foundry responses. The property-name mappings are the only
/// behavior in these DTOs, so round-trip and raw-JSON tests fully exercise them.
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
    public void NonAIGeocodingResponse_DeserializesResultsArray()
    {
        const string json = """
        {
          "results": [
            {
              "name": "Nashville",
              "admin1": "Tennessee",
              "country": "United States",
              "latitude": 36.16589,
              "longitude": -86.78444
            }
          ]
        }
        """;

        var result = JsonSerializer.Deserialize<NonAIGeocodingResponse>(json);

        Assert.NotNull(result);
        var first = Assert.Single(result!.Results);
        Assert.Equal("Nashville", first.Name);
        Assert.Equal("Tennessee", first.Admin1);
        Assert.Equal("United States", first.Country);
        Assert.Equal(36.16589, first.Latitude);
        Assert.Equal(-86.78444, first.Longitude);
    }

    [Fact]
    public void NonAIGeocodingResponse_NoResultsKey_DefaultsToEmptyList()
    {
        var result = JsonSerializer.Deserialize<NonAIGeocodingResponse>("{}");

        Assert.NotNull(result);
        Assert.Empty(result!.Results);
    }

    [Fact]
    public void NonAILatLongResponse_RoundTripsThroughJson()
    {
        var original = new NonAILatLongResponse
        {
            Name = "Toronto",
            Latitude = 43.7,
            Longitude = -79.42,
        };

        var json = JsonSerializer.Serialize(original);
        Assert.Contains("\"name\":\"Toronto\"", json);
        Assert.Contains("\"latitude\":43.7", json);

        var roundTripped = JsonSerializer.Deserialize<NonAILatLongResponse>(json);
        Assert.NotNull(roundTripped);
        Assert.Equal(original.Name, roundTripped!.Name);
        Assert.Equal(original.Latitude, roundTripped.Latitude);
        Assert.Equal(original.Longitude, roundTripped.Longitude);
    }

    [Fact]
    public void AIWeatherResponse_DeserializesFoundryAgentSchema()
    {
        const string json = """
        {
          "fullSummary": "It is 41F in Nashville with light winds from the south.",
          "temperatureF": 41,
          "windSpeedMPH": 7.5,
          "windDirection": "S",
          "conditions": "Partly cloudy"
        }
        """;

        var result = JsonSerializer.Deserialize<AIWeatherResponse>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.Equal("It is 41F in Nashville with light winds from the south.", result!.FullSummary);
        Assert.Equal(41, result.TemperatureF);
        Assert.Equal(7.5, result.WindSpeedMPH);
        Assert.Equal("S", result.WindDirection);
        Assert.Equal("Partly cloudy", result.Conditions);
    }

    [Fact]
    public void AIWeatherResponse_DefaultsStringFieldsToEmpty()
    {
        var result = new AIWeatherResponse();

        Assert.Equal(string.Empty, result.FullSummary);
        Assert.Equal(string.Empty, result.WindDirection);
        Assert.Equal(string.Empty, result.Conditions);
    }
}
