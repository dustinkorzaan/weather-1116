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

    [Fact]
    public void AIWeatherResponse_DefaultsRunLogDetailsToEmptyList()
    {
        var result = new AIWeatherResponse();

        Assert.Empty(result.RunLogDetails);
    }

    [Fact]
    public void AIWeatherResponse_SerializesRunLogDetailsCamelCaseContract()
    {
        var json = JsonSerializer.Serialize(new AIWeatherResponse
        {
            RunLogDetails =
            [
                new RunLogDetail
                {
                    DateTimeUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    LoopNumber = 1,
                    Message = "Start loop 1",
                    InputTokenCount = 42,
                    CachedTokenCount = 10,
                    OutputTokenCount = 20,
                    ReasoningTokenCount = 5,
                    TotalTokenCount = 62,
                    RuntimeMs = 100,
                    LoopRuntimeMs = 50,
                    RunningTotalMs = 150,
                },
            ],
        });

        using var document = JsonDocument.Parse(json);
        var entry = document.RootElement.GetProperty("runLogDetails")[0];

        Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), entry.GetProperty("dateTimeUtc").GetDateTime());
        Assert.Equal(1, entry.GetProperty("loopNumber").GetInt32());
        Assert.Equal("Start loop 1", entry.GetProperty("message").GetString());
        Assert.Equal(42, entry.GetProperty("inputTokenCount").GetInt32());
        Assert.Equal(10, entry.GetProperty("cachedTokenCount").GetInt32());
        Assert.Equal(20, entry.GetProperty("outputTokenCount").GetInt32());
        Assert.Equal(5, entry.GetProperty("reasoningTokenCount").GetInt32());
        Assert.Equal(62, entry.GetProperty("totalTokenCount").GetInt32());
        Assert.Equal(100, entry.GetProperty("runtimeMs").GetInt32());
        Assert.Equal(50, entry.GetProperty("loopRuntimeMs").GetInt32());
        Assert.Equal(150, entry.GetProperty("runningTotalMs").GetInt32());
        Assert.False(entry.TryGetProperty("LoopNumber", out _));
    }
}
