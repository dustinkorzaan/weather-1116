using System.Text.Json;
using Core.AIWeather.Models;

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
          "conditions": "Partly cloudy",
          "locationName": "Nashville, TN",
          "latitude": 36.1627,
          "longitude": -86.7816
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
        Assert.Equal("Nashville, TN", result.LocationName);
        Assert.Equal(36.1627, result.Latitude);
        Assert.Equal(-86.7816, result.Longitude);
    }

    [Fact]
    public void AIWeatherResponse_DefaultsStringFieldsToEmpty()
    {
        var result = new AIWeatherResponse();

        Assert.Equal(string.Empty, result.FullSummary);
        Assert.Equal(string.Empty, result.WindDirection);
        Assert.Equal(string.Empty, result.Conditions);
        Assert.Equal(string.Empty, result.LocationName);
        Assert.Equal(0, result.Latitude);
        Assert.Equal(0, result.Longitude);
    }
}
