using System.Net;
using System.Net.Http.Json;
using Core.About;

namespace WeatherMcpSrvAppService.Tests;

public class AboutControllerTests : IClassFixture<WeatherMcpSrvAppServiceWebApplicationFactory>
{
    private readonly WeatherMcpSrvAppServiceWebApplicationFactory _factory;

    public AboutControllerTests(WeatherMcpSrvAppServiceWebApplicationFactory factory)
    {
        _factory = factory.WithSetting("MCP_SRV_APP_SERVICE_KEY", "integration-test-mcp-key");
    }

    [Fact]
    public async Task Get_ReturnsHealthyMcpSrvAppServiceNode_WhenKeyAndToolConfigured()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/About");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var node = await response.Content.ReadFromJsonAsync<AboutNode>();
        Assert.NotNull(node);
        Assert.Equal("mcp-srv-app-service", node.Name);
        Assert.True(node.IsHealthy);
        Assert.Empty(node.Children);
    }

    [Fact]
    public async Task Get_ReturnsUnhealthyNode_WhenMcpSrvAppServiceKeyMissing()
    {
        using var factory = new WeatherMcpSrvAppServiceWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/About");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var node = await response.Content.ReadFromJsonAsync<AboutNode>();
        Assert.NotNull(node);
        Assert.Equal("mcp-srv-app-service", node.Name);
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
