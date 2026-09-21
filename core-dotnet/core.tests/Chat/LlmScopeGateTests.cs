namespace Core.Tests.Chat;

public class LlmScopeGateTests
{
    private static readonly string Source =
        File.ReadAllText(RepoFiles.FindRepoFile("core-dotnet/core/Chat/Services/ChatScopeGate/LlmScopeGate.cs"));

    [Fact]
    public void Gate_IssuesItsOwnResponsesCallRatherThanReusingTheOrchestrationAgent()
    {
        Assert.Contains("CreateResponseAsync", Source, StringComparison.Ordinal);
        Assert.Contains("Chat5ScopeClassifierPrompt", Source, StringComparison.Ordinal);
        Assert.DoesNotContain("orchestrationAgent", Source, StringComparison.Ordinal);
        Assert.DoesNotContain("RunStreamingAsync", Source, StringComparison.Ordinal);
    }

    [Fact]
    public void Gate_ParsesInScopeVerdictFromTheStartOfTheResponseText()
    {
        Assert.Contains("StartsWith(\"IN_SCOPE\"", Source, StringComparison.Ordinal);
    }
}
