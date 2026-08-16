namespace Core.Tests.AIWeather.Handlers;

public class GetCurrentAIWeatherHandlerTests
{
    [Fact]
    public void SystemPrompt_UsesFriendlySummaryWithoutLatLong()
    {
        var prompt = File.ReadAllText(FindRepoFile("core-dotnet/core/AIWeather/Handlers/GetCurrentAIWeatherHandler.cs"));

        Assert.Contains("one or two friendly sentences describing the current weather", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("place name", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("human-friendly city name", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Do not include latitude or longitude in fullSummary", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("place name, latitude, longitude", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("- latitude:", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("- longitude:", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("- windDirectionDegrees:", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("temperature", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("wind speed", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("wind direction", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("conditions", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GitHub-flavored Markdown", prompt);
        Assert.Contains("also JSON fields", prompt);
        Assert.DoesNotContain("Exactly one sentence", prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("AIWeatherSystemInstructions", prompt, StringComparison.Ordinal);
        Assert.Contains("Use U.S. customary units only: °F, mph, and \" (e.g. 72°F, 8 mph, 1\"). Do not use C, KPH, or MM.", prompt);
    }

    private static string FindRepoFile(string relativePath)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException($"Could not find {relativePath} from {AppContext.BaseDirectory}");
    }
}
