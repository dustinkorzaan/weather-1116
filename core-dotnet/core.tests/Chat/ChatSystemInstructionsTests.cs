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
        Assert.Contains("GetCities", prompt);
        Assert.Contains("Report distances in miles", prompt);
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
    public void WeatherAssistant_KnowsTheSavedPinTools()
    {
        var prompt = ChatSystemInstructions.WeatherAssistant;

        Assert.Contains("GetUser", prompt);
        Assert.Contains("AddUserCity", prompt);
        Assert.Contains("DeleteUserCity", prompt);
        Assert.Contains("never guess an id", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MultiAgentAiWeatherOrchestrationAssistant_DelegatesPinsToUser()
    {
        var prompt = ChatSystemInstructions.MultiAgentAiWeatherOrchestrationAssistant;

        Assert.Contains("exactly three tools", prompt);
        Assert.DoesNotContain("exactly two tools", prompt);
        Assert.Contains("User lists the user's saved cities", prompt);
        Assert.Contains("call Geo first for its coordinates, then ask User to add the city", prompt);
        Assert.Contains("never guess a city id", prompt, StringComparison.OrdinalIgnoreCase);
        // The orchestrator only delegates; the saved-city tools themselves belong to the User agent.
        Assert.DoesNotContain("AddUserCity", prompt);
        Assert.DoesNotContain("DeleteUserCity", prompt);
    }

    [Fact]
    public void MultiAgentUserAssistant_IsPinsOnly()
    {
        var prompt = ChatSystemInstructions.MultiAgentUserAssistant;

        Assert.Contains("GetUser", prompt);
        Assert.Contains("AddUserCity", prompt);
        Assert.Contains("DeleteUserCity", prompt);
        Assert.Contains("never guess or invent an id", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("GetLatLong", prompt);
        Assert.DoesNotContain("GetPublicWeather", prompt);
        Assert.DoesNotContain("°F", prompt);
    }

    [Fact]
    public void MultiAgentAiWeatherOrchestrationAssistant_DelegatesToGeoAndNonAiWeather()
    {
        var prompt = ChatSystemInstructions.MultiAgentAiWeatherOrchestrationAssistant;

        Assert.Contains("Geo", prompt);
        Assert.Contains("NonAI Weather", prompt);
        Assert.Contains("largest cities", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("pass every city through to the user", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Never guess a location or weather fact yourself", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GitHub-flavored Markdown", prompt);
    }

    [Fact]
    public void MultiAgentAiWeatherOrchestrationAssistant_RequiresNumericCoordinatesResentEveryCall()
    {
        var prompt = ChatSystemInstructions.MultiAgentAiWeatherOrchestrationAssistant;

        Assert.Contains("numeric coordinates", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no memory of its own", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("every call", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("a location or coordinates", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MultiAgentAiWeatherOrchestrationAssistant_KnowsNonAiWeatherResolutionTiers()
    {
        var prompt = ChatSystemInstructions.MultiAgentAiWeatherOrchestrationAssistant;

        Assert.Contains("hourly", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("every 15 minutes", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MultiAgentGeoAssistant_IsGeoOnly()
    {
        var prompt = ChatSystemInstructions.MultiAgentGeoAssistant;

        Assert.Contains("latitude", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GetLatLong", prompt);
        Assert.Contains("GetLocation", prompt);
        Assert.Contains("GetCities", prompt);
        Assert.Contains("For GetCities, answer with every returned city", prompt);
        Assert.DoesNotContain("°F", prompt);
        Assert.DoesNotContain("GetPublicWeather", prompt);
        Assert.DoesNotContain("UserCity", prompt);
    }

    [Fact]
    public void MultiAgentNonAiWeatherAssistant_IsWeatherOnlyWithUnitRules()
    {
        var prompt = ChatSystemInstructions.MultiAgentNonAiWeatherAssistant;

        Assert.DoesNotContain("GetCities", prompt);

        Assert.Contains("°F", prompt);
        Assert.Contains("mph", prompt);
        Assert.Contains("GetPublicWeatherCurrent", prompt);
        Assert.Contains("GetPublicWeatherForecast", prompt);
        Assert.Contains("GetPublicWeatherHistory", prompt);
        Assert.DoesNotContain("GetLatLong", prompt);
        Assert.DoesNotContain("UserCity", prompt);
    }
}
