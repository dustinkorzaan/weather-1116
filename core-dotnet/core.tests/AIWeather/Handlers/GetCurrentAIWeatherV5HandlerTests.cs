namespace Core.Tests.AIWeather.Handlers;

public class GetCurrentAIWeatherV5HandlerTests
{
    [Fact]
    public void Handler_CallsHostedAgentWithUserPromptOnlyAndNoLocalToolLoop()
    {
        var source = File.ReadAllText(RepoFiles.FindRepoFile("core-dotnet/core/AIWeather/Handlers/GetCurrentAIWeatherV5Handler.cs"));

        Assert.Contains("FoundryAgentResponsesClientFactory.CreateForAgentAsync", source, StringComparison.Ordinal);
        Assert.Contains("new Uri(projectUrl)", source, StringComparison.Ordinal);
        Assert.Contains("response is null", source, StringComparison.Ordinal);
        Assert.Contains("AZURE_FOUNDRY_PROD_CURRENT_WX_AGENT_NAME", source, StringComparison.Ordinal);
        // An empty (but set) GitHub var must still fall back to the default agent name --
        // ?? only catches null, not "", so this has to be an explicit blank check.
        Assert.Contains("IsNullOrWhiteSpace(agentNameEnv)", source, StringComparison.Ordinal);
        Assert.Contains("AIWeatherResponse", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ChatMcpToolFactory", source, StringComparison.Ordinal);
        Assert.DoesNotContain("WeatherToolExecutor", source, StringComparison.Ordinal);
        Assert.DoesNotContain("WeatherToolDefinitions", source, StringComparison.Ordinal);
        Assert.DoesNotContain("MaxToolLoopIterations", source, StringComparison.Ordinal);
        Assert.DoesNotContain("do\n        {", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateMcpApprovalResponseItem", source, StringComparison.Ordinal);
        Assert.DoesNotContain("StoredOutputEnabled", source, StringComparison.Ordinal);
        Assert.Contains("require_approval: never", source, StringComparison.Ordinal);
        // Instructions, response schema, and MCP tools live on the hosted agent - this handler
        // has no local schema to build, unlike V3/V4.
        Assert.DoesNotContain("BuildAIOutputSchema", source, StringComparison.Ordinal);
        Assert.DoesNotContain("JsonSchemaExporter", source, StringComparison.Ordinal);
        Assert.DoesNotContain("TextOptions", source, StringComparison.Ordinal);
        Assert.Contains("ConversationOptions = new ResponseConversationOptions()", source, StringComparison.Ordinal);
        Assert.Contains("AgentConversationId = conversationId", source, StringComparison.Ordinal);
        Assert.Contains("CreateResponseStreamingAsync", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Handler_RecordsRunLog()
    {
        var source = File.ReadAllText(RepoFiles.FindRepoFile("core-dotnet/core/AIWeather/Handlers/GetCurrentAIWeatherV5Handler.cs"));

        Assert.Contains("runLog.AddLog(", source, StringComparison.Ordinal);
        Assert.Contains("modelOutput.RunLogDetails = runLog.Hydrate();", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AddLog(1,", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Handler_WrapsRequestInCatchAll_SoAnyExceptionStillLogsAPairedResponseRow()
    {
        var source = File.ReadAllText(RepoFiles.FindRepoFile("core-dotnet/core/AIWeather/Handlers/GetCurrentAIWeatherV5Handler.cs"));

        // A stray "await LogActivityErrorAsync(...)" sprinkled before an explicit throw would mean
        // an exception thrown by CreateResponseAsync itself (network/auth failure, not one of the
        // handler's own validation checks) skips logging entirely, leaving the Request row logged
        // above unpaired. The single catch-all is what guarantees every exit path is covered.
        Assert.Contains("catch (Exception ex)", source, StringComparison.Ordinal);
        Assert.Contains("ErrorMessage = ex.Message,", source, StringComparison.Ordinal);
        Assert.DoesNotContain("LogActivityErrorAsync", source, StringComparison.Ordinal);
    }

}
