using Core.Chat.Services;

namespace Core.Tests.Chat;

public class ChatSystemInstructionsTests
{
    [Fact]
    public void WeatherAssistant_UsesFriendlySummaryWithoutLatLong()
    {
        var prompt = ChatSystemInstructions.WeatherAssistant;

        Assert.Contains("GitHub-flavored Markdown", prompt);
        Assert.Contains("When you report current weather, use one or two friendly sentences", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("place name", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("place name, latitude, longitude", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("latitude/longitude", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GetLocation", prompt);
        Assert.Contains("GetPublicWeatherCurrent", prompt);
        Assert.Contains("GetPublicWeatherForecast", prompt);
        Assert.Contains("GetPublicWeatherHistory", prompt);
        Assert.Contains("Do not emit raw HTML", prompt);
        Assert.Contains("Use U.S. customary units only: °F, mph, and \" (e.g. 72°F, 8 mph, 1\"). Do not use C, KPH, or MM.", prompt);
    }
}
