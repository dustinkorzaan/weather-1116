using System.Text.Json;
using Core.AIWeather.Models;
using Core.Json;

namespace Core.Tests.AIWeather.Models;

/// <summary>
/// Verifies JSON contracts for model output (<see cref="AIWeatherModelResponse"/>)
/// and API responses (<see cref="AIWeatherResponse"/>).
/// </summary>
public class AIWeatherModelsTests
{
    [Fact]
    public void AIWeatherModelResponse_DeserializesStrictModelSchema()
    {
        const string json = """
        {
          "fullSummary": "It is 41F in Nashville with light winds from the south.",
          "temperatureF": 41,
          "windSpeedMPH": 7.5,
          "windDirection": "S",
          "windDirectionFromDegrees": 180,
          "conditions": "Partly cloudy",
          "latitude": 36.1627,
          "longitude": -86.7816
        }
        """;

        var result = JsonSerializer.Deserialize<AIWeatherModelResponse>(
            json,
            JsonDefaults.CaseInsensitive);

        Assert.NotNull(result);
        Assert.Equal(180, result!.WindDirectionFromDegrees);
        Assert.Equal("S", result.WindDirection);
    }

    [Fact]
    public void AIWeatherModelResponse_ToApiResponse_ConvertsFromDegreesToTowards()
    {
        var api = new AIWeatherModelResponse
        {
            FullSummary = "Windy.",
            TemperatureF = 72,
            WindSpeedMPH = 8,
            WindDirection = "S",
            WindDirectionFromDegrees = 180,
            Conditions = "Clear",
            Latitude = 36.16,
            Longitude = -86.78,
        }.ToApiResponse();

        Assert.Equal(0, api.WindDirectionTowardsDegrees);
        Assert.Equal("N", api.WindDirection);
    }

    [Fact]
    public void AIWeatherResponse_DeserializesApiContract()
    {
        const string json = """
        {
          "fullSummary": "It is 41F in Nashville with light winds from the south.",
          "temperatureF": 41,
          "windSpeedMPH": 7.5,
          "windDirection": "N",
          "windDirectionTowardsDegrees": 0,
          "conditions": "Partly cloudy",
          "latitude": 36.1627,
          "longitude": -86.7816
        }
        """;

        var result = JsonSerializer.Deserialize<AIWeatherResponse>(
            json,
            JsonDefaults.CaseInsensitive);

        Assert.NotNull(result);
        Assert.Equal("N", result!.WindDirection);
        Assert.Equal(0, result.WindDirectionTowardsDegrees);
    }

    [Fact]
    public void AIWeatherResponse_SerializesCamelCaseContract()
    {
        var json = JsonSerializer.Serialize(new AIWeatherResponse
        {
            FullSummary = "Sunny.",
            TemperatureF = 100,
            WindSpeedMPH = 13,
            WindDirection = "SW",
            WindDirectionTowardsDegrees = 224,
            Conditions = "Hot",
            Latitude = 36.16,
            Longitude = -86.78,
        });

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal(100, root.GetProperty("temperatureF").GetDouble());
        Assert.Equal(13, root.GetProperty("windSpeedMPH").GetDouble());
        Assert.Equal("SW", root.GetProperty("windDirection").GetString());
        Assert.Equal(224, root.GetProperty("windDirectionTowardsDegrees").GetInt32());
        Assert.Equal(36.16, root.GetProperty("latitude").GetDouble());
        Assert.Equal(-86.78, root.GetProperty("longitude").GetDouble());
        Assert.False(root.TryGetProperty("TemperatureF", out _));
        Assert.False(root.TryGetProperty("windDirectionFromDegrees", out _));
        Assert.False(root.TryGetProperty("windDirectionToDegrees", out _));
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
