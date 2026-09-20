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
    public void Service_AppendsToHistoryAfterGatesRunSoAGateThrowingDoesNotRecordAnOrphanedMessage()
    {
        var gatesIndex = Source.IndexOf("RunInputGatesAsync(request", StringComparison.Ordinal);
        var appendIndex = Source.IndexOf("_sessionStore.AppendMessage(sessionId, new Models.ChatMessage { Role = \"user\"", StringComparison.Ordinal);
        var blockedIndex = Source.IndexOf("if (blockedReason is not null)", StringComparison.Ordinal);

        Assert.True(gatesIndex >= 0);
        Assert.True(appendIndex >= 0);
        Assert.True(blockedIndex >= 0);
        Assert.True(gatesIndex < appendIndex, "The user message must be appended only after gates have run, so a gate error doesn't record an orphaned message.");
        Assert.True(appendIndex < blockedIndex, "The user message must still be appended before the blocked check, so a blocked message is recorded.");
    }

    [Fact]
    public void Service_UsesInProcessMediatorToolsLikeChat4a()
    {
        Assert.Contains("IMediator", Source, StringComparison.Ordinal);
        Assert.DoesNotContain("ChatHostedMcpToolFactory", Source, StringComparison.Ordinal);
    }
}
