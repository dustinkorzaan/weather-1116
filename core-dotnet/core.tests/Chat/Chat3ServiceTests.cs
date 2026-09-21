namespace Core.Tests.Chat;

public class Chat3ServiceTests
{
    [Fact]
    public void Service_SendsUserPromptOnlyAndDoesNotRoundTripMcpApprovals()
    {
        var source = File.ReadAllText(RepoFiles.FindRepoFile("core-dotnet/core/Chat/Chat3/Chat3Service.cs"));

        Assert.Contains("CreateProjectResponsesClientForChatAgent", source, StringComparison.Ordinal);
        Assert.Contains("AZURE_FOUNDRY_PROD_CHAT_AGENT_NAME", source, StringComparison.Ordinal);
        Assert.Contains("require_approval: never", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateMcpApprovalResponseItem", source, StringComparison.Ordinal);
        Assert.DoesNotContain("auto-approving", source, StringComparison.Ordinal);
        Assert.DoesNotContain("pendingApprovals", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ChatMcpToolFactory", source, StringComparison.Ordinal);
        Assert.DoesNotContain("WeatherToolExecutor", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ConversationOptions", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AgentConversationId", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Service_CatchesStreamingFailuresDuringEnumeration()
    {
        var source = File.ReadAllText(RepoFiles.FindRepoFile("core-dotnet/core/Chat/Chat3/Chat3Service.cs"));

        // CreateResponseStreamingAsync is lazy — Foundry/auth failures happen on first MoveNext,
        // so the try/catch must wrap await foreach, not just the call that builds the enumerable.
        Assert.Contains("await enumerator.MoveNextAsync()", source, StringComparison.Ordinal);
        Assert.Contains("ExceptionDispatchInfo.Capture", source, StringComparison.Ordinal);
        Assert.Contains("ChatStreamEvent.Error(failure.SourceException.Message)", source, StringComparison.Ordinal);
    }

}
