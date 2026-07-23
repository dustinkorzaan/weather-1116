using System.Net;
using System.Net.Http.Json;
using Core.about;

namespace WeatherAPI.Tests;

public class AboutControllerTests(WeatherApiWebApplicationFactory factory) : IClassFixture<WeatherApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Get_ReturnsApiRootTree()
    {
        var response = await _client.GetAsync("/About");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var root = await response.Content.ReadFromJsonAsync<AboutNode>();
        Assert.NotNull(root);
        Assert.Equal("API Root", root.Name);
        Assert.True(root.IsHealthy);
        Assert.Contains(root.Children, child => child.Name == "API");
        Assert.Contains(root.Children, child => child.Name == "mcp-dotnet" && child.IsHealthy);
        Assert.Contains(root.Children, child => child.Name == "mcp-function" && child.IsHealthy);
        Assert.Contains(root.Children, child => child.Name == "worker-dotnet" && child.IsHealthy);
        Assert.Equal(4, root.Children.Count);
    }
}
