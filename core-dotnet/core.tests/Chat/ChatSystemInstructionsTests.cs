using Core.Chat.Services;

namespace Core.Tests.Chat;

public class ChatSystemInstructionsTests
{
    [Fact]
    public void WeatherAssistant_UsesFriendlySummaryWithoutLatLong()
    {
        var prompt = ChatSystemInstructions.WeatherAssistant;

        Assert.Contains("GitHub-flavored Markdown", prompt);
        Assert.Contains("one or two friendly sentences", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("place name", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("place name, latitude, longitude", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("latitude/longitude", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GetLocation", prompt);
        Assert.Contains("GetPublicWeatherCurrent", prompt);
        Assert.Contains("GetPublicWeatherForecast", prompt);
        Assert.Contains("GetPublicWeatherHistory", prompt);
        Assert.Contains("Do not emit raw HTML", prompt);
    }
}
