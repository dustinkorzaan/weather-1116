using Core.AIWeather.Services;

namespace Core.Tests.AIWeather.Services;

public class FoundryOpenAiEndpointTests
{
    [Theory]
    [InlineData(
        "https://wx1116-prd-res-eu2.services.ai.azure.com/api/projects/wx1116-prd-prj-eu2",
        "https://wx1116-prd-res-eu2.services.ai.azure.com/api/projects/wx1116-prd-prj-eu2/openai/v1")]
    [InlineData(
        "https://wx1116-prd-res-eu2.services.ai.azure.com/api/projects/wx1116-prd-prj-eu2/",
        "https://wx1116-prd-res-eu2.services.ai.azure.com/api/projects/wx1116-prd-prj-eu2/openai/v1")]
    [InlineData(
        "https://wx1116-prd-res-eu2.services.ai.azure.com/openai/v1",
        "https://wx1116-prd-res-eu2.services.ai.azure.com/openai/v1")]
    [InlineData(
        "https://wx1116-prd-res-eu2.services.ai.azure.com/openai/v1/",
        "https://wx1116-prd-res-eu2.services.ai.azure.com/openai/v1")]
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
}
