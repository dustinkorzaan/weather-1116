namespace Core.Tests.Chat;

public class Chat3ServiceTests
{
    [Fact]
    public void Service_SendsUserPromptOnlyAndDoesNotRoundTripMcpApprovals()
    {
        var source = File.ReadAllText(RepoFiles.FindRepoFile("core-dotnet/core/Chat/Chat3/Chat3Service.cs"));

        Assert.Contains("CreateProjectResponsesClientForChatAgentAsync", source, StringComparison.Ordinal);
        Assert.Contains("AZURE_FOUNDRY_PROD_CHAT_AGENT_NAME", source, StringComparison.Ordinal);
        Assert.Contains("require_approval: never", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateMcpApprovalResponseItem", source, StringComparison.Ordinal);
        Assert.DoesNotContain("auto-approving", source, StringComparison.Ordinal);
        Assert.DoesNotContain("pendingApprovals", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ChatMcpToolFactory", source, StringComparison.Ordinal);
        Assert.DoesNotContain("WeatherToolExecutor", source, StringComparison.Ordinal);
        Assert.Contains("ConversationOptions = new ResponseConversationOptions()", source, StringComparison.Ordinal);
        Assert.Contains("AgentConversationId = conversationId", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Service_ReusesSessionConversationWithoutPreviousResponseId()
    {
        var source = File.ReadAllText(RepoFiles.FindRepoFile("core-dotnet/core/Chat/Chat3/Chat3Service.cs"));

        // Foundry returns HTTP 400 "Cannot provide both 'previous_response_id' and 'conversation'"
        // if a later turn sends both, so later turns continue the stored conversation instead.
        Assert.Contains("_responseStore.GetConversationId(sessionId)", source, StringComparison.Ordinal);
        Assert.Contains("_responseStore.SetConversationId(sessionId, conversationId)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("options.PreviousResponseId", source, StringComparison.Ordinal);
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
        Assert.Contains("CreateProjectResponsesClientForChatAgentAsync", source, StringComparison.Ordinal);
        Assert.Contains("Chat3 failed to create Foundry conversation", source, StringComparison.Ordinal);
    }

}
