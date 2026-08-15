using System.Net;
using System.Net.Http.Json;
using Core.About;

namespace WeatherMVC.Tests;

public class AboutControllerTests(WeatherMvcWebApplicationFactory factory) : IClassFixture<WeatherMvcWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Get_ReturnsMvcRootTree()
    {
        var response = await _client.GetAsync("/About");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var root = await response.Content.ReadFromJsonAsync<AboutNode>();
        Assert.NotNull(root);
        Assert.Equal("MVC Root", root.Name);
        Assert.True(root.IsHealthy);
        Assert.Equal(2, root.Children.Count);
        Assert.Equal("MVC", root.Children[0].Name);

        var apiRoot = root.Children[1];
        Assert.Equal("API Root", apiRoot.Name);
        Assert.Equal("API", apiRoot.Children[0].Name);
        Assert.Contains(apiRoot.Children, child => child.Name == "mcp-srv-app-service" && child.IsHealthy);
        Assert.Contains(apiRoot.Children, child => child.Name == "mcp-srv-func-app" && child.IsHealthy);
        Assert.Contains(apiRoot.Children, child => child.Name == "Worker Root" && child.IsHealthy);
        Assert.Equal(4, apiRoot.Children.Count);
    }
}
