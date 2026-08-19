using System.Text.Json;
using Core.AIWeather.Models;
using Core.Json;

namespace Core.Tests.AIWeather.Models;

/// <summary>
/// Verifies JSON contracts for <see cref="AIWeatherResponse"/> (model output and API).
/// </summary>
public class AIWeatherModelsTests
{
    [Fact]
    public void AIWeatherResponse_DeserializesApiContract()
    {
        const string json = """
        {
          "fullSummary": "It is 41F in Nashville with light winds from the south.",
          "temperatureF": 41,
          "windSpeedMPH": 7.5,
          "windDirectionSource": "S",
          "windDirectionSourceDegrees": 180,
          "conditions": "Partly cloudy",
          "latitude": 36.1627,
          "longitude": -86.7816
        }
        """;

        var result = JsonSerializer.Deserialize<AIWeatherResponse>(
            json,
            JsonDefaults.CaseInsensitive);

        Assert.NotNull(result);
        Assert.Equal("S", result!.WindDirectionSource);
        Assert.Equal(180, result.WindDirectionSourceDegrees);
    }

    [Fact]
    public void AIWeatherResponse_SerializesCamelCaseContract()
    {
        var json = JsonSerializer.Serialize(new AIWeatherResponse
        {
            FullSummary = "Sunny.",
            TemperatureF = 100,
            WindSpeedMPH = 13,
            WindDirectionSource = "SW",
            WindDirectionSourceDegrees = 224,
            Conditions = "Hot",
            Latitude = 36.16,
            Longitude = -86.78,
        });

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal(100, root.GetProperty("temperatureF").GetDouble());
        Assert.Equal(13, root.GetProperty("windSpeedMPH").GetDouble());
        Assert.Equal("SW", root.GetProperty("windDirectionSource").GetString());
        Assert.Equal(224, root.GetProperty("windDirectionSourceDegrees").GetInt32());
        Assert.Equal(36.16, root.GetProperty("latitude").GetDouble());
        Assert.Equal(-86.78, root.GetProperty("longitude").GetDouble());
        Assert.False(root.TryGetProperty("TemperatureF", out _));
        Assert.False(root.TryGetProperty("windDirection", out _));
        Assert.False(root.TryGetProperty("windDirectionTowardsDegrees", out _));
        Assert.False(root.TryGetProperty("windDirectionFromDegrees", out _));
        Assert.False(root.TryGetProperty("windDirectionToDegrees", out _));
    }

    [Fact]
    public void AIWeatherResponse_DefaultsStringFieldsToEmpty()
    {
        var result = new AIWeatherResponse();

        Assert.Equal(string.Empty, result.FullSummary);
        Assert.Equal(string.Empty, result.WindDirectionSource);
        Assert.Equal(string.Empty, result.Conditions);
    }
}
