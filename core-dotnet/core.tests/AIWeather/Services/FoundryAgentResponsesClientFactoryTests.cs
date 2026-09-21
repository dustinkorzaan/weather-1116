namespace Core.Tests.AIWeather.Services;

public class FoundryAgentResponsesClientFactoryTests
{
    [Fact]
    public void Factory_MatchesFoundryConsoleV5ClientConstruction()
    {
        var factory = File.ReadAllText(
            RepoFiles.FindRepoFile("core-dotnet/core/AIWeather/Services/FoundryAgentResponsesClientFactory.cs"));
        var console = File.ReadAllText(RepoFiles.FindRepoFile("FoundryConsoleV5/Program.cs"));

        Assert.Contains("Endpoint = endpoint", factory, StringComparison.Ordinal);
        Assert.Contains("Endpoint = new Uri(endpoint)", console, StringComparison.Ordinal);
        Assert.Contains("GetProjectResponsesClientForAgent", factory, StringComparison.Ordinal);
        Assert.Contains("GetProjectResponsesClientForAgent", console, StringComparison.Ordinal);
        Assert.Contains("CreateProjectConversationAsync", factory, StringComparison.Ordinal);
        Assert.Contains("CreateProjectConversationAsync", console, StringComparison.Ordinal);
        Assert.DoesNotContain("AgentName =", factory, StringComparison.Ordinal);
        Assert.DoesNotContain("FoundryOpenAiEndpoint.Resolve", factory, StringComparison.Ordinal);
        Assert.DoesNotContain("ResolveProjectEndpoint", factory, StringComparison.Ordinal);
    }
}
