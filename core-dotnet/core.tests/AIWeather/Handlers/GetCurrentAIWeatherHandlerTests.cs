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
        Assert.Contains("- windDirectionToDegrees:", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Do not add 180", prompt, StringComparison.Ordinal);
        Assert.Contains("temperature", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("wind speed", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("wind direction", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("conditions", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GitHub-flavored Markdown", prompt);
        Assert.Contains("also JSON fields", prompt);
        Assert.DoesNotContain("Exactly one sentence", prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("AIWeatherSystemInstructions", prompt, StringComparison.Ordinal);
        Assert.Contains("Use U.S. customary units only: °F, mph, and \" (e.g. 72°F, 8 mph, 1\"). Convert from the weather tool's native units (°C, km/h, mm). Do not present C, KPH, or MM in responses.", prompt);
    }

    [Fact]
    public void Handler_UsesInProcessToolLoopNotRemoteMcp()
    {
        var source = File.ReadAllText(FindRepoFile("core-dotnet/core/AIWeather/Handlers/GetCurrentAIWeatherHandler.cs"));

        Assert.Contains("WeatherToolExecutor", source, StringComparison.Ordinal);
        Assert.Contains("WeatherToolDefinitions", source, StringComparison.Ordinal);
        Assert.Contains("MeteorologicalFromToWindTo", source, StringComparison.Ordinal);
        Assert.Contains("MaxToolLoopTurns = 32", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateMcpTool", source, StringComparison.Ordinal);
        Assert.DoesNotContain("MCP_SRV_", source, StringComparison.Ordinal);
        Assert.DoesNotContain("System prompt for {Location}", source, StringComparison.Ordinal);
        Assert.DoesNotContain("User prompt for {Location}", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Output schema for {Location}", source, StringComparison.Ordinal);
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
