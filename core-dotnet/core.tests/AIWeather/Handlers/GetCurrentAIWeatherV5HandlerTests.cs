namespace Core.Tests.AIWeather.Handlers;

public class GetCurrentAIWeatherV5HandlerTests
{
    [Fact]
    public void Handler_CallsHostedAgentWithUserPromptOnlyAndNoLocalToolLoop()
    {
        var source = File.ReadAllText(FindRepoFile("core-dotnet/core/AIWeather/Handlers/GetCurrentAIWeatherV5Handler.cs"));

        Assert.Contains("ProjectOpenAIClient", source, StringComparison.Ordinal);
        Assert.Contains("GetProjectResponsesClientForAgent", source, StringComparison.Ordinal);
        Assert.Contains("AZURE_FOUNDRY_PROD_EUS2_AGENT_NAME", source, StringComparison.Ordinal);
        Assert.Contains("AIWeatherResponse", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ChatMcpToolFactory", source, StringComparison.Ordinal);
        Assert.DoesNotContain("WeatherToolExecutor", source, StringComparison.Ordinal);
        Assert.DoesNotContain("WeatherToolDefinitions", source, StringComparison.Ordinal);
        Assert.DoesNotContain("MaxToolLoopTurns", source, StringComparison.Ordinal);
        Assert.DoesNotContain("do\n        {", source, StringComparison.Ordinal);
        // Instructions, response schema, and MCP tools live on the hosted agent - this handler
        // has no local schema to build, unlike V3/V4.
        Assert.DoesNotContain("BuildAIOutputSchema", source, StringComparison.Ordinal);
        Assert.DoesNotContain("JsonSchemaExporter", source, StringComparison.Ordinal);
        Assert.DoesNotContain("TextOptions", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Handler_RecordsRunLog()
    {
        var source = File.ReadAllText(FindRepoFile("core-dotnet/core/AIWeather/Handlers/GetCurrentAIWeatherV5Handler.cs"));

        Assert.Contains("runLog.AddLog(", source, StringComparison.Ordinal);
        Assert.Contains("modelOutput.RunLogDetails = runLog.HydrateRuntimes();", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AddLog(1,", source, StringComparison.Ordinal);
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
