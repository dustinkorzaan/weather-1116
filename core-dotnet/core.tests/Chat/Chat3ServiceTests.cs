namespace Core.Tests.Chat;

public class Chat3ServiceTests
{
    [Fact]
    public void Service_SendsUserPromptOnlyAndDoesNotRoundTripMcpApprovals()
    {
        var source = File.ReadAllText(RepoFiles.FindRepoFile("core-dotnet/core/Chat/Chat3/Chat3Service.cs"));

        Assert.Contains("CreateProjectResponsesClientForChatAgent", source, StringComparison.Ordinal);
        Assert.Contains("AZURE_FOUNDRY_PROD_EUS2_CHAT_AGENT_NAME", source, StringComparison.Ordinal);
        Assert.Contains("require_approval: never", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateMcpApprovalResponseItem", source, StringComparison.Ordinal);
        Assert.DoesNotContain("auto-approving", source, StringComparison.Ordinal);
        Assert.DoesNotContain("pendingApprovals", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ChatMcpToolFactory", source, StringComparison.Ordinal);
        Assert.DoesNotContain("WeatherToolExecutor", source, StringComparison.Ordinal);
    }

}
