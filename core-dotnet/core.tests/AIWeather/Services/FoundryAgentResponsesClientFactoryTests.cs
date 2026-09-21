namespace Core.Tests.AIWeather.Services;

public class FoundryAgentResponsesClientFactoryTests
{
    [Fact]
    public void Factory_SetsAgentName_SoSdkAddsApiVersionQuery()
    {
        var source = File.ReadAllText(
            RepoFiles.FindRepoFile("core-dotnet/core/AIWeather/Services/FoundryAgentResponsesClientFactory.cs"));

        // Azure.AI.Extensions.OpenAI 3.0.0-beta.2 only appends ?api-version= when AgentName is set.
        // Without it, project-root conversations/responses calls return HTTP 400
        // "Missing required query parameter: api-version".
        Assert.Contains("AgentName = agentName", source, StringComparison.Ordinal);
        Assert.Contains("FoundryOpenAiEndpoint.Resolve", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ResolveProjectEndpoint", source, StringComparison.Ordinal);
        Assert.Contains("GetProjectResponsesClientForAgent", source, StringComparison.Ordinal);
        Assert.Contains("CreateProjectConversationAsync", source, StringComparison.Ordinal);
    }
}
