using System.Net;

namespace WeatherMVC.Tests;

public class HomeControllerTests(WeatherMvcWebApplicationFactory factory) : IClassFixture<WeatherMvcWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Index_ReturnsOkWithMapAndWithoutPresentationContent()
    {
        var response = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("id=\"weather-map\"", html);
        Assert.Contains("href=\"/presentation\"", html);
        Assert.DoesNotContain("Hello, from WeatherMVC", html);
        Assert.DoesNotContain("Chat Clients", html);
    }

    [Fact]
    public async Task Presentation_ReturnsOkAtCanonicalRoute()
    {
        var response = await _client.GetAsync("/presentation");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Hello, from WeatherMVC", html);
        Assert.Contains("Chat Clients", html);
        Assert.DoesNotContain("id=\"weather-map\"", html);
    }

    [Fact]
    public async Task Presentation_ReturnsOkAtConventionalRoute()
    {
        var response = await _client.GetAsync("/Home/Presentation");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Hello, from WeatherMVC", html);
        Assert.Contains("Chat Clients", html);
        Assert.DoesNotContain("id=\"weather-map\"", html);
    }
}
