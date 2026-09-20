namespace Core.Tests.Chat;

public class Chat5aServiceTests
{
    private static readonly string Source =
        File.ReadAllText(RepoFiles.FindRepoFile("core-dotnet/core/Chat/Chat5a/Chat5aService.cs"));

    [Fact]
    public void Service_RunsInputGatesBeforeBuildingTheOrchestrationAgent()
    {
        var gatesIndex = Source.IndexOf("RunInputGatesAsync(request", StringComparison.Ordinal);
        var buildIndex = Source.IndexOf("BuildOrchestrationAgent(responsesClient", StringComparison.Ordinal);

        Assert.True(gatesIndex >= 0);
        Assert.True(buildIndex >= 0);
        Assert.True(gatesIndex < buildIndex, "Input gates must run before the orchestration agent is built.");
    }

    [Fact]
    public void Service_SkipsStreamingWhenOutputGateIsEnabled()
    {
        Assert.Contains("if (request.EnableLlmOutputGate)", Source, StringComparison.Ordinal);
        Assert.Contains("RunAsync(userMessage, agentSession, cancellationToken: cancellationToken)", Source, StringComparison.Ordinal);
        Assert.Contains("RunStreamingAsync(userMessage, agentSession, cancellationToken: cancellationToken)", Source, StringComparison.Ordinal);
    }

    [Fact]
    public void Service_UsesHardenedPromptOnlyWhenSystemPromptGuardIsEnabled()
    {
        Assert.Contains("useHardenedPrompt", Source, StringComparison.Ordinal);
        Assert.Contains("Chat5HardenedAiWeatherOrchestrationAssistant", Source, StringComparison.Ordinal);
        Assert.Contains("request.EnableSystemPromptGuard", Source, StringComparison.Ordinal);
    }

    [Fact]
    public void Service_BlockedMessagesAreStillAppendedToSessionHistoryBeforeGatesRun()
    {
        var appendIndex = Source.IndexOf("_sessionStore.AppendMessage(sessionId, new Models.ChatMessage { Role = \"user\"", StringComparison.Ordinal);
        var gatesIndex = Source.IndexOf("RunInputGatesAsync(request", StringComparison.Ordinal);

        Assert.True(appendIndex >= 0);
        Assert.True(gatesIndex >= 0);
        Assert.True(appendIndex < gatesIndex, "The user message must be saved to history before any gate can block it.");
    }

    [Fact]
    public void Service_UsesInProcessMediatorToolsLikeChat4a()
    {
        Assert.Contains("IMediator", Source, StringComparison.Ordinal);
        Assert.DoesNotContain("ChatHostedMcpToolFactory", Source, StringComparison.Ordinal);
    }
}
