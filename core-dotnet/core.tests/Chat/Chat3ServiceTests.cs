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
    public void Service_SetsConversationOptions_SoApplyClientDefaultsDoesNotThrow()
    {
        var source = File.ReadAllText(RepoFiles.FindRepoFile("core-dotnet/core/Chat/Chat3/Chat3Service.cs"));

        // ProjectResponsesClient.CreateResponseStreamingAsync reads AgentConversationId (via
        // ApplyClientDefaults) before every call, which walks into ConversationOptions.Patch and
        // throws a NullReferenceException in CreateResponseOptions.PropagateGet when
        // ConversationOptions is left at its default null. A non-null ConversationOptions on the
        // options literal is required to avoid it.
        Assert.Contains("ConversationOptions = new ResponseConversationOptions()", source, StringComparison.Ordinal);

        // Setting ConversationOptions alone isn't enough: if AgentConversationId still reads null,
        // ApplyClientDefaults writes it back as null, which removes "$.conversation" - and that
        // removal propagates onto ConversationOptions' own patch in a way that throws a
        // KeyNotFoundException ("No value found at JSON path '$'") from
        // ResponseConversationOptions' JSON writer the next time options is serialized. Giving
        // AgentConversationId a real value up front avoids that second failure too.
        Assert.Contains("options.AgentConversationId = sessionId;", source, StringComparison.Ordinal);
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
