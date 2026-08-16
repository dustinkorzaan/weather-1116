using Core.Chat.Services;

namespace Core.Tests.Chat;

public class ChatSystemInstructionsTests
{
    [Fact]
    public void WeatherAssistant_AllowsMarkdownAndIncludesLocationFacts()
    {
        var prompt = ChatSystemInstructions.WeatherAssistant;

        Assert.Contains("GitHub-flavored Markdown", prompt);
        Assert.Contains("one or two sentences", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("place name", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("latitude", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("longitude", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GetLocationData", prompt);
        Assert.Contains("Do not emit raw HTML", prompt);
    }
}
