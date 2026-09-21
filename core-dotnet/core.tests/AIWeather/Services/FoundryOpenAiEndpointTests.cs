using Core.AIWeather.Services;

namespace Core.Tests.AIWeather.Services;

public class FoundryOpenAiEndpointTests
{
    [Theory]
    [InlineData(
        "https://example.services.ai.azure.com/api/projects/wx1116-prod-proj",
        "https://example.services.ai.azure.com/api/projects/wx1116-prod-proj/openai/v1")]
    [InlineData(
        "https://example.services.ai.azure.com/api/projects/wx1116-prod-proj/",
        "https://example.services.ai.azure.com/api/projects/wx1116-prod-proj/openai/v1")]
    [InlineData(
        "https://example.services.ai.azure.com/openai/v1",
        "https://example.services.ai.azure.com/openai/v1")]
    [InlineData(
        "https://example.services.ai.azure.com/openai/v1/",
        "https://example.services.ai.azure.com/openai/v1")]
    public void Resolve_AppendsOpenAiPathWhenMissing(string input, string expected)
    {
        var endpoint = FoundryOpenAiEndpoint.Resolve(input);

        Assert.Equal(expected, endpoint.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_ThrowsForBlankUrl(string input)
    {
        Assert.Throws<ArgumentException>(() => FoundryOpenAiEndpoint.Resolve(input));
    }

    [Fact]
    public void Resolve_ThrowsForNullUrl()
    {
        Assert.Throws<ArgumentNullException>(() => FoundryOpenAiEndpoint.Resolve(null!));
    }

    [Theory]
    [InlineData(
        "https://example.services.ai.azure.com/api/projects/wx1116-prod-proj",
        "https://example.services.ai.azure.com/api/projects/wx1116-prod-proj")]
    [InlineData(
        "https://example.services.ai.azure.com/api/projects/wx1116-prod-proj/openai/v1",
        "https://example.services.ai.azure.com/api/projects/wx1116-prod-proj")]
    [InlineData(
        "https://example.services.ai.azure.com/api/projects/wx1116-prod-proj/openai/v1/",
        "https://example.services.ai.azure.com/api/projects/wx1116-prod-proj")]
    public void ResolveProjectEndpoint_StripsOpenAiSuffixWhenPresent(string input, string expected)
    {
        var endpoint = FoundryOpenAiEndpoint.ResolveProjectEndpoint(input);

        Assert.Equal(expected, endpoint.ToString());
    }

    [Fact]
    public void ProjectOpenAiApiVersion_IsSetForProjectOpenAIClient()
    {
        Assert.Equal("2025-11-15-preview", FoundryOpenAiEndpoint.ProjectOpenAiApiVersion);
    }

    [Fact]
    public void CreateProjectOpenAIClientOptions_SetsAgentNameAndApiVersionForSdkPipeline()
    {
        var options = FoundryOpenAiEndpoint.CreateProjectOpenAIClientOptions(
            new Uri("https://example.services.ai.azure.com/api/projects/wx1116-prod-proj"),
            "wx1116-agent-for-current-weather");

        Assert.Equal("wx1116-agent-for-current-weather", options.AgentName);
        Assert.Equal(FoundryOpenAiEndpoint.ProjectOpenAiApiVersion, options.ApiVersion);
    }
}
