using System.Text.Json;
using Core.AIWeather.Models;
using Core.Json;

namespace Core.Tests.AIWeather.Models;

/// <summary>
/// Verifies the JSON contract for AI weather responses returned by the hosted model.
/// </summary>
public class AIWeatherModelsTests
{
    [Fact]
    public void AIWeatherResponse_DeserializesFoundryAgentSchema()
    {
        const string json = """
        {
          "fullSummary": "It is 41F in Nashville with light winds from the south.",
          "temperatureF": 41,
          "windSpeedMPH": 7.5,
          "windDirection": "S",
          "windDirectionDegrees": 180,
          "conditions": "Partly cloudy",
          "latitude": 36.1627,
          "longitude": -86.7816
        }
        """;

        var result = JsonSerializer.Deserialize<AIWeatherResponse>(
            json,
            JsonDefaults.CaseInsensitive);

        Assert.NotNull(result);
        Assert.Equal("It is 41F in Nashville with light winds from the south.", result!.FullSummary);
        Assert.Equal(41, result.TemperatureF);
        Assert.Equal(7.5, result.WindSpeedMPH);
        Assert.Equal("S", result.WindDirection);
        Assert.Equal(180, result.WindDirectionDegrees);
        Assert.Equal("Partly cloudy", result.Conditions);
        Assert.Equal(36.1627, result.Latitude);
        Assert.Equal(-86.7816, result.Longitude);
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
            WindDirectionDegrees = 224,
            Conditions = "Hot",
            Latitude = 36.16,
            Longitude = -86.78,
        });

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal(100, root.GetProperty("temperatureF").GetDouble());
        Assert.Equal(13, root.GetProperty("windSpeedMPH").GetDouble());
        Assert.Equal("SW", root.GetProperty("windDirection").GetString());
        Assert.Equal(224, root.GetProperty("windDirectionDegrees").GetInt32());
        Assert.Equal(36.16, root.GetProperty("latitude").GetDouble());
        Assert.Equal(-86.78, root.GetProperty("longitude").GetDouble());
        Assert.False(root.TryGetProperty("TemperatureF", out _));
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
