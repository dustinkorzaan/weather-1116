using Core.AIWeather;

namespace Core.Tests.AIWeather;

public class FoundryOpenAiEndpointTests
{
	[Theory]
	[InlineData(
		"https://wx1116-prd-res-eu2.services.ai.azure.com/api/projects/wx1116-prd-prj-eu2",
		"https://wx1116-prd-res-eu2.services.ai.azure.com/openai/v1")]
	[InlineData(
		"https://wx1116-prd-res-eu2.services.ai.azure.com/api/projects/wx1116-prd-prj-eu2/openai/v1",
		"https://wx1116-prd-res-eu2.services.ai.azure.com/openai/v1")]
	[InlineData(
		"https://wx1116-prd-res-eu2.services.ai.azure.com/openai/v1",
		"https://wx1116-prd-res-eu2.services.ai.azure.com/openai/v1")]
	public void ResolveForModelDirect_UsesResourceScopedOpenAiEndpoint(string input, string expected)
	{
		var endpoint = FoundryOpenAiEndpoint.ResolveForModelDirect(input);

		Assert.Equal(expected, endpoint.ToString());
	}

	[Theory]
	[InlineData(
		"https://wx1116-prd-res-eu2.services.ai.azure.com/api/projects/wx1116-prd-prj-eu2",
		"https://wx1116-prd-res-eu2.services.ai.azure.com/api/projects/wx1116-prd-prj-eu2/openai/v1")]
	[InlineData(
		"https://wx1116-prd-res-eu2.services.ai.azure.com/api/projects/wx1116-prd-prj-eu2/openai/v1",
		"https://wx1116-prd-res-eu2.services.ai.azure.com/api/projects/wx1116-prd-prj-eu2/openai/v1")]
	public void ResolveForProjectOpenAi_UsesProjectScopedOpenAiEndpoint(string input, string expected)
	{
		var endpoint = FoundryOpenAiEndpoint.ResolveForProjectOpenAi(input);

		Assert.Equal(expected, endpoint.ToString());
	}

	[Fact]
	public void ResolveForHostedAgent_BuildsAgentProtocolEndpoint()
	{
		const string projectUrl = "https://wx1116-prd-res-eu2.services.ai.azure.com/api/projects/wx1116-prd-prj-eu2";
		const string agentName = "wx1116-agent-default";

		var endpoint = FoundryOpenAiEndpoint.ResolveForHostedAgent(projectUrl, agentName);

		Assert.Equal(
			"https://wx1116-prd-res-eu2.services.ai.azure.com/api/projects/wx1116-prd-prj-eu2/agents/wx1116-agent-default/endpoint/protocols/openai",
			endpoint.ToString());
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public void ResolveForModelDirect_ThrowsForBlankUrl(string input)
	{
		Assert.Throws<ArgumentException>(() => FoundryOpenAiEndpoint.ResolveForModelDirect(input));
	}

	[Fact]
	public void ResolveForModelDirect_ThrowsForNullUrl()
	{
		Assert.Throws<ArgumentNullException>(() => FoundryOpenAiEndpoint.ResolveForModelDirect(null!));
	}

	[Fact]
	public void ResolveForHostedAgent_ThrowsWhenProjectUrlMissing()
	{
		Assert.Throws<InvalidOperationException>(() =>
			FoundryOpenAiEndpoint.ResolveForHostedAgent("https://wx1116-prd-res-eu2.services.ai.azure.com/openai/v1", "agent"));
	}
}
