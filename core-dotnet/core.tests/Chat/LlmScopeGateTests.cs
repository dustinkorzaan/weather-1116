namespace Core.Tests.Chat;

public class LlmScopeGateTests
{
    private static readonly string Source =
        File.ReadAllText(RepoFiles.FindRepoFile("core-dotnet/core/Chat/Services/ChatScopeGate/LlmScopeGate.cs"));

    [Fact]
    public void Gate_IssuesItsOwnResponsesCallRatherThanReusingTheOrchestrationAgent()
    {
        Assert.Contains("CreateResponseAsync", Source, StringComparison.Ordinal);
        Assert.Contains("_classifierPrompt", Source, StringComparison.Ordinal);
        Assert.DoesNotContain("orchestrationAgent", Source, StringComparison.Ordinal);
        Assert.DoesNotContain("RunStreamingAsync", Source, StringComparison.Ordinal);
    }

    [Fact]
    public void Gate_TakesItsClassifierPromptThroughTheConstructorRatherThanHardcodingOne()
    {
        // Regression guard: gate #3 ("LLM Input") and gate #5 ("LLM Output") are registered with
        // two different prompts (Chat5InputScopeClassifierPrompt / Chat5OutputScopeClassifierPrompt
        // in ChatServiceCollectionExtensions) because a request is a question and a reply is a
        // statement. If EvaluateAsync went back to hardcoding one of those constants directly,
        // both registrations would silently share it again.
        Assert.DoesNotContain("Instructions = ChatSystemInstructions.", Source, StringComparison.Ordinal);
        Assert.Contains("Instructions = _classifierPrompt", Source, StringComparison.Ordinal);
        Assert.Contains("string classifierPrompt", Source, StringComparison.Ordinal);
    }

    [Fact]
    public void Gate_ParsesInScopeVerdictFromTheStartOfTheResponseText()
    {
        Assert.Contains("StartsWith(\"IN_SCOPE\"", Source, StringComparison.Ordinal);
    }

    [Fact]
    public void Registrations_WireEachGateToItsOwnClassifierPrompt()
    {
        var wiring = File.ReadAllText(
            RepoFiles.FindRepoFile("core-dotnet/core/Chat/ChatServiceCollectionExtensions.cs"));

        Assert.Contains("\"LLM Input\", ChatSystemInstructions.Chat5InputScopeClassifierPrompt", wiring, StringComparison.Ordinal);
        Assert.Contains("\"LLM Output\", ChatSystemInstructions.Chat5OutputScopeClassifierPrompt", wiring, StringComparison.Ordinal);
    }
}
