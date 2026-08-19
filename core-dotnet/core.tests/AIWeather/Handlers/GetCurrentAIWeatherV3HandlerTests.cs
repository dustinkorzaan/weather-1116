namespace Core.Tests.AIWeather.Handlers;

public class GetCurrentAIWeatherV3HandlerTests
{
    [Fact]
    public void SystemPrompt_UsesFriendlySummaryWithoutLatLong()
    {
        var prompt = File.ReadAllText(FindRepoFile("core-dotnet/core/AIWeather/Handlers/GetCurrentAIWeatherV3Handler.cs"));

        Assert.Contains("one or two friendly sentences describing the current weather", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("place name", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("human-friendly city name", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Do not include latitude or longitude in fullSummary", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("place name, latitude, longitude", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("- latitude:", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("- longitude:", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("- windDirectionSourceDegrees:", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("- windDirectionSource:", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("- windDirection:", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("current_weather.winddirection", prompt, StringComparison.Ordinal);
        Assert.Contains("Do not add 180", prompt, StringComparison.Ordinal);
        Assert.Contains("16-point compass", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AIWeatherSystemInstructions", prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("AIWeatherModelResponse", prompt, StringComparison.Ordinal);
        Assert.Contains("WeatherUnitConversion.NormalizeSourceDegrees", prompt, StringComparison.Ordinal);
        Assert.Contains("WeatherUnitConversion.DegreesToCompass", prompt, StringComparison.Ordinal);
        Assert.Contains("windDirectionSource", prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("- windDirectionTowardsDegrees:", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("- windDirectionFromDegrees:", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("temperature", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("wind speed", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("wind direction", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("conditions", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GitHub-flavored Markdown", prompt);
        Assert.Contains("also JSON fields", prompt);
        Assert.DoesNotContain("Exactly one sentence", prompt, StringComparison.Ordinal);
        Assert.Contains("normally rank 1", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("from the best result", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("best geo result", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Fetch weather for that location only", prompt, StringComparison.Ordinal);
        Assert.Contains("Use U.S. customary units only: °F, mph, and \" (e.g. 72°F, 8 mph, 1\"). Convert from the weather tool's native units (°C, km/h, mm). Do not present C, KPH, or MM in responses.", prompt);
    }

    [Fact]
    public void Handler_UsesInProcessToolLoopNotRemoteMcp()
    {
        var source = File.ReadAllText(FindRepoFile("core-dotnet/core/AIWeather/Handlers/GetCurrentAIWeatherV3Handler.cs"));

        Assert.Contains("WeatherToolExecutor", source, StringComparison.Ordinal);
        Assert.Contains("WeatherToolDefinitions", source, StringComparison.Ordinal);
        Assert.Contains("AIWeatherResponse", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AIWeatherModelResponse", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ToApiResponse", source, StringComparison.Ordinal);
        Assert.DoesNotContain("properties.Remove(\"windDirectionSource\")", source, StringComparison.Ordinal);
        Assert.Contains("MaxToolLoopTurns = 32", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateMcpTool", source, StringComparison.Ordinal);
        Assert.DoesNotContain("MCP_SRV_", source, StringComparison.Ordinal);
        Assert.DoesNotContain("System prompt for {Location}", source, StringComparison.Ordinal);
        Assert.DoesNotContain("User prompt for {Location}", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Output schema for {Location}", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Handler_RecordsRunLogAndExcludesItFromModelSchema()
    {
        var source = File.ReadAllText(FindRepoFile("core-dotnet/core/AIWeather/Handlers/GetCurrentAIWeatherV3Handler.cs"));

        Assert.Contains("runLog.AddLog(", source, StringComparison.Ordinal);
        Assert.Contains("properties.Remove(\"runLogDetails\")", source, StringComparison.Ordinal);
        Assert.Contains("modelOutput.RunLogDetails = runLog.HydrateRuntimes();", source, StringComparison.Ordinal);
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
