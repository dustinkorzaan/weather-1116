using Core.AIWeather;

namespace Core.Tests.AIWeather;

public class AIWeatherSystemInstructionsTests
{
    [Fact]
    public void WindDirectionJsonFields_ExplainsSourceDegreesAndCompassHydration()
    {
        var fields = AIWeatherSystemInstructions.WindDirectionJsonFields;

        Assert.Contains("- windDirectionSourceDegrees:", fields, StringComparison.Ordinal);
        Assert.Contains("- windDirectionSource:", fields, StringComparison.Ordinal);
        Assert.Contains("current_weather.winddirection", fields, StringComparison.Ordinal);
        Assert.Contains("Do not add 180", fields, StringComparison.Ordinal);
        Assert.Contains("16-point compass", fields, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("22.5", fields, StringComparison.Ordinal);
        Assert.Contains("224 → SW", fields, StringComparison.Ordinal);
    }

    [Fact]
    public void WindDirectionSummaryGuidance_UsesSourceCompassLabel()
    {
        var guidance = AIWeatherSystemInstructions.WindDirectionSummaryGuidance;

        Assert.Contains("windDirectionSource", guidance, StringComparison.Ordinal);
        Assert.Contains("meteorological source", guidance, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Do not add 180", guidance, StringComparison.Ordinal);
    }
}
