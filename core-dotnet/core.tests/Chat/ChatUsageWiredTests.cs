using Core.Chat.Models;

namespace Core.Tests.Chat;

public class ChatUsageWiredTests
{
    [Theory]
    [InlineData("core-dotnet/core/Chat/Chat1a/Chat1aService.cs")]
    [InlineData("core-dotnet/core/Chat/Chat1b/Chat1bService.cs")]
    [InlineData("core-dotnet/core/Chat/Chat2a/Chat2aService.cs")]
    [InlineData("core-dotnet/core/Chat/Chat2b/Chat2bService.cs")]
    [InlineData("core-dotnet/core/Chat/Chat3/Chat3Service.cs")]
    public void Service_AccumulatesUsageOnDone(string relativePath)
    {
        var source = File.ReadAllText(RepoFiles.FindRepoFile(relativePath));

        Assert.Contains("new ChatUsageAccumulator()", source, StringComparison.Ordinal);
        Assert.Contains("ChatStreamEvent.Done(usage.ToChatUsage())", source, StringComparison.Ordinal);
    }
}
