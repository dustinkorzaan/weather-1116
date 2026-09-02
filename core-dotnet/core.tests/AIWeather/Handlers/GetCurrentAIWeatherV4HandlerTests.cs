namespace Core.Tests.AIWeather.Handlers;

public class GetCurrentAIWeatherV4HandlerTests
{
    [Fact]
    public void SystemPrompt_UsesFriendlySummaryWithoutLatLong()
    {
        var prompt = File.ReadAllText(RepoFiles.FindRepoFile("core-dotnet/core/AIWeather/Handlers/GetCurrentAIWeatherV4Handler.cs"));

        Assert.Contains("one or two friendly sentences describing the current weather", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("place name", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("human-friendly city name", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Do not include latitude or longitude in fullSummary", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("- latitude:", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("- longitude:", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("- windDirectionSourceDegrees:", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("- windDirectionSource:", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("current_weather.winddirection", prompt, StringComparison.Ordinal);
        Assert.Contains("16-point compass", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("WeatherUnitConversion.NormalizeSourceDegrees", prompt, StringComparison.Ordinal);
        Assert.Contains("WeatherUnitConversion.DegreesToCompass", prompt, StringComparison.Ordinal);
        Assert.Contains("Use U.S. customary units only: °F, mph, and \" (e.g. 72°F, 8 mph, 1\"). Convert from the weather tool's native units (°C, km/h, mm). Do not present C, KPH, or MM in responses.", prompt);
    }

    [Fact]
    public void Handler_UsesRemoteMcpToolsWithNoLocalToolLoop()
    {
        var source = File.ReadAllText(RepoFiles.FindRepoFile("core-dotnet/core/AIWeather/Handlers/GetCurrentAIWeatherV4Handler.cs"));

        Assert.Contains("ChatMcpToolFactory", source, StringComparison.Ordinal);
        Assert.Contains("_mcpToolFactory.CreateTools()", source, StringComparison.Ordinal);
        Assert.Contains("AIWeatherResponse", source, StringComparison.Ordinal);
        Assert.DoesNotContain("WeatherToolExecutor", source, StringComparison.Ordinal);
        Assert.DoesNotContain("WeatherToolDefinitions", source, StringComparison.Ordinal);
        Assert.DoesNotContain("MaxToolLoopIterations", source, StringComparison.Ordinal);
        Assert.DoesNotContain("do\n        {", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Handler_RecordsRunLogAndExcludesItFromModelSchema()
    {
        var source = File.ReadAllText(RepoFiles.FindRepoFile("core-dotnet/core/AIWeather/Handlers/GetCurrentAIWeatherV4Handler.cs"));

        Assert.Contains("runLog.AddLog(", source, StringComparison.Ordinal);
        Assert.Contains("properties.Remove(\"runLogDetails\")", source, StringComparison.Ordinal);
        Assert.Contains("modelOutput.RunLogDetails = runLog.Hydrate();", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AddLog(1,", source, StringComparison.Ordinal);
    }

}
