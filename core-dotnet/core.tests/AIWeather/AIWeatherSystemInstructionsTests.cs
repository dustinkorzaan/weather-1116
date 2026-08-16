using Core.AIWeather;

namespace Core.Tests.AIWeather;

public class AIWeatherSystemInstructionsTests
{
    [Fact]
    public void CurrentWeatherJson_AllowsMarkdownAndIncludesLocationFacts()
    {
        var prompt = AIWeatherSystemInstructions.CurrentWeatherJson;

        Assert.Contains("one or two sentences", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("place name", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("latitude", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("longitude", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("temperature", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("wind speed", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("wind direction", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("conditions", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GitHub-flavored Markdown", prompt);
        Assert.Contains("also JSON fields", prompt);
        Assert.DoesNotContain("Exactly one sentence", prompt, StringComparison.Ordinal);
    }
}
