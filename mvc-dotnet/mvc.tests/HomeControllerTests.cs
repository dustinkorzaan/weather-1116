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

    [Fact]
    public async Task Layout_UsesHandWrittenCssAndSemanticClasses()
    {
        var response = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("css/site.css", html);
        Assert.Contains("class=\"site-header\"", html);
        Assert.Contains("class=\"avatar-menu\"", html);
        Assert.Contains("class=\"site-main\"", html);
        Assert.DoesNotContain("bg-blue-800", html);
        Assert.DoesNotContain("flex-1", html);
        Assert.DoesNotContain("rounded-xl", html);
    }

    [Fact]
    public async Task Presentation_UsesSemanticClassesForChatAndWeather()
    {
        var response = await _client.GetAsync("/presentation");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("class=\"page-shell\"", html);
        Assert.Contains("class=\"weather-card\"", html);
        Assert.Contains("class=\"chat-tab is-active\"", html);
        Assert.Contains("class=\"btn\"", html);
        Assert.DoesNotContain("btn-primary", html);
        Assert.DoesNotContain("bg-blue-700", html);
        Assert.DoesNotContain("grid-cols-1", html);
    }

    [Fact]
    public async Task SiteCss_IsHandWrittenNotTailwind()
    {
        var response = await _client.GetAsync("/css/site.css");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var css = await response.Content.ReadAsStringAsync();
        Assert.Contains(".site-header", css);
        Assert.Contains("#weather-map", css);
        Assert.Contains("[hidden]", css);
        Assert.DoesNotContain("tailwindcss", css, StringComparison.OrdinalIgnoreCase);
    }
}
