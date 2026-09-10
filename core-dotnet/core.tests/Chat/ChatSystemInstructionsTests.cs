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
        Assert.Contains("Use U.S. customary units only: °F, mph, and \" (e.g. 72°F, 8 mph, 1\"). Convert from the weather tool's native units (°C, km/h, mm). Do not present C, KPH, or MM in responses.", prompt);
        Assert.Contains("windDirectionSource", prompt);
        Assert.Contains("meteorological source", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Do not add 180", prompt);
    }

    [Fact]
    public void MultiAgentAiWeatherOrchestrationAssistant_DelegatesToGeoAndNonAiWeather()
    {
        var prompt = ChatSystemInstructions.MultiAgentAiWeatherOrchestrationAssistant;

        Assert.Contains("Geo", prompt);
        Assert.Contains("NonAI Weather", prompt);
        Assert.Contains("Never guess a location or weather fact yourself", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GitHub-flavored Markdown", prompt);
    }

    [Fact]
    public void MultiAgentGeoAssistant_IsGeoOnly()
    {
        var prompt = ChatSystemInstructions.MultiAgentGeoAssistant;

        Assert.Contains("latitude", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GetLatLong", prompt);
        Assert.Contains("GetLocation", prompt);
        Assert.DoesNotContain("°F", prompt);
        Assert.DoesNotContain("GetPublicWeather", prompt);
    }

    [Fact]
    public void MultiAgentNonAiWeatherAssistant_IsWeatherOnlyWithUnitRules()
    {
        var prompt = ChatSystemInstructions.MultiAgentNonAiWeatherAssistant;

        Assert.Contains("°F", prompt);
        Assert.Contains("mph", prompt);
        Assert.Contains("GetPublicWeatherCurrent", prompt);
        Assert.Contains("GetPublicWeatherForecast", prompt);
        Assert.Contains("GetPublicWeatherHistory", prompt);
        Assert.DoesNotContain("GetLatLong", prompt);
    }
}
