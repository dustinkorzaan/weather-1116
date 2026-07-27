using System.Net;
using System.Net.Http.Json;
using Core.About;

namespace WeatherMcpDotNet.Tests;

public class AboutControllerTests : IClassFixture<WeatherMcpDotNetWebApplicationFactory>
{
    private readonly WeatherMcpDotNetWebApplicationFactory _factory;

    public AboutControllerTests(WeatherMcpDotNetWebApplicationFactory factory)
    {
        _factory = factory.WithSetting("MCP_API_KEY", "integration-test-mcp-key");
    }

    [Fact]
    public async Task Get_ReturnsHealthyMcpDotNetNode_WhenKeyAndToolConfigured()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/About");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var node = await response.Content.ReadFromJsonAsync<AboutNode>();
        Assert.NotNull(node);
        Assert.Equal("mcp-dotnet", node.Name);
        Assert.True(node.IsHealthy);
        Assert.Empty(node.Children);
    }

    [Fact]
    public async Task Get_ReturnsUnhealthyNode_WhenMcpApiKeyMissing()
    {
        using var factory = new WeatherMcpDotNetWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/About");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var node = await response.Content.ReadFromJsonAsync<AboutNode>();
        Assert.NotNull(node);
        Assert.Equal("mcp-dotnet", node.Name);
        Assert.False(node.IsHealthy);
    }

    [Fact]
    public async Task McpEndpoint_ReturnsUnauthorized_WithoutBearerToken()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/mcp");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
