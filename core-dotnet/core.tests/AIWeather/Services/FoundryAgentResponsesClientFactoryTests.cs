namespace Core.Tests.AIWeather.Services;

public class FoundryAgentResponsesClientFactoryTests
{
    [Fact]
    public void Factory_PassesEnvUrlAsIs_LikeFoundryConsoleV5()
    {
        var source = File.ReadAllText(
            RepoFiles.FindRepoFile("core-dotnet/core/AIWeather/Services/FoundryAgentResponsesClientFactory.cs"));

        Assert.DoesNotContain("ResolveProjectEndpoint", source, StringComparison.Ordinal);
        Assert.Contains("GetProjectResponsesClientForAgent", source, StringComparison.Ordinal);
        Assert.Contains("AZURE_FOUNDRY_PROD_PROJ_URL", source, StringComparison.Ordinal);
        Assert.Contains("new Uri(endpoint)", source, StringComparison.Ordinal);
        Assert.Contains("CreateProjectConversationAsync", source, StringComparison.Ordinal);
    }
}
