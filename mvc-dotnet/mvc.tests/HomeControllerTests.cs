using System.Net;

namespace WeatherMVC.Tests;

public class HomeControllerTests(WeatherMvcWebApplicationFactory factory) : IClassFixture<WeatherMvcWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Index_ReturnsOkWithMapAndWithoutSplitPageContent()
    {
        var response = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("id=\"weather-map\"", html);
        Assert.Contains("href=\"/hello-world\"", html);
        Assert.Contains("href=\"/current-ai-weather\"", html);
        Assert.Contains("href=\"/chat-clients\"", html);
        Assert.Contains("Login/Logout", html);
        Assert.DoesNotContain("href=\"/presentation\"", html);
        Assert.DoesNotContain("Hello, from WeatherMVC", html);
        Assert.DoesNotContain("id=\"ai-weather-form\"", html);
        Assert.DoesNotContain("id=\"chat-messages\"", html);
        Assert.DoesNotContain("Hello World</h2>", html);
        Assert.DoesNotContain("Current AI Weather</h2>", html);
        Assert.DoesNotContain("Chat Clients</h2>", html);
    }

    [Fact]
    public async Task HelloWorld_ReturnsOkAtCanonicalRoute()
    {
        var response = await _client.GetAsync("/hello-world");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Hello, from WeatherMVC", html);
        Assert.Contains("Hello World", html);
        Assert.DoesNotContain("Current AI Weather</h2>", html);
        Assert.DoesNotContain("Chat Clients</h2>", html);
        Assert.DoesNotContain("id=\"weather-map\"", html);
    }

    [Fact]
    public async Task CurrentAIWeather_ReturnsOkAtCanonicalRoute()
    {
        var response = await _client.GetAsync("/current-ai-weather");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Current AI Weather", html);
        Assert.Contains("class=\"weather-card\"", html);
        Assert.Contains("class=\"btn\"", html);
        Assert.DoesNotContain("Hello World</h2>", html);
        Assert.DoesNotContain("Chat Clients</h2>", html);
        Assert.DoesNotContain("id=\"weather-map\"", html);
        Assert.DoesNotContain("btn-primary", html);
        Assert.DoesNotContain("bg-blue-700", html);
        Assert.DoesNotContain("grid-cols-1", html);
    }

    [Fact]
    public async Task ChatClients_ReturnsOkAtCanonicalRoute()
    {
        var response = await _client.GetAsync("/chat-clients");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Chat Clients", html);
        Assert.Contains("class=\"page-shell\"", html);
        Assert.Contains("class=\"chat-tab is-active\"", html);
        Assert.DoesNotContain("Hello World</h2>", html);
        Assert.DoesNotContain("Current AI Weather</h2>", html);
        Assert.DoesNotContain("id=\"weather-map\"", html);
    }

    [Fact]
    public async Task Presentation_IsNotARoute()
    {
        var response = await _client.GetAsync("/presentation");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
        Assert.Contains("stroke-width=\"2.25\"", html);
        Assert.DoesNotContain("bg-blue-800", html);
        Assert.DoesNotContain("flex-1", html);
        Assert.DoesNotContain("rounded-xl", html);
    }

    [Fact]
    public async Task SiteCss_IsHandWrittenNotTailwind()
    {
        var response = await _client.GetAsync("/css/site.css");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var css = await response.Content.ReadAsStringAsync();
        Assert.Contains(".site-header", css);
        Assert.Contains("border: 2px solid var(--color-border-strong)", css);
        Assert.Contains("#weather-map", css);
        Assert.Contains("[hidden]", css);
        Assert.DoesNotContain("tailwindcss", css, StringComparison.OrdinalIgnoreCase);
    }
}
