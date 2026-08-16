using System.Net;

namespace WeatherMVC.Tests;

public class HomeControllerTests(WeatherMvcWebApplicationFactory factory) : IClassFixture<WeatherMvcWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static string FindRepoFile(string relativePath)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException($"Could not find {relativePath} from {AppContext.BaseDirectory}");
    }

    [Fact]
    public async Task Index_ReturnsOkWithMapAndWithoutSplitPageContent()
    {
        var response = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("id=\"weather-map\"", html);
        Assert.Contains("aria-label=\"Add location\"", html);
        Assert.Contains("href=\"/hello-world\"", html);
        Assert.Contains("href=\"/current-ai-weather\"", html);
        Assert.Contains("href=\"/chat-clients\"", html);
        Assert.Contains("Login/Logout", html);
        Assert.Contains("data-theme-option=\"light\"", html);
        Assert.Contains("data-theme-option=\"dark\"", html);
        Assert.Contains("data-theme-option=\"system\"", html);
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
        Assert.DoesNotContain("aria-label=\"Add location\"", html);
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
        Assert.DoesNotContain("aria-label=\"Add location\"", html);
        Assert.DoesNotContain("Hello World</h2>", html);
        Assert.DoesNotContain("Chat Clients</h2>", html);
        Assert.DoesNotContain("id=\"weather-map\"", html);
        Assert.DoesNotContain("btn-primary", html);
        Assert.DoesNotContain("bg-blue-700", html);
        Assert.DoesNotContain("grid-cols-1", html);
    }

    [Fact]
    public async Task CurrentAIWeather_ReturnsOkWithLocationQuery()
    {
        var response = await _client.GetAsync("/current-ai-weather?location=nashville%20tn");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("id=\"ai-weather-form\"", html);
        Assert.Contains("id=\"ai-weather-location\"", html);
        Assert.Contains("currentAIWeather.js", html);
    }

    [Fact]
    public void IndexView_UsesCityAndStateMapPinLabels()
    {
        var view = File.ReadAllText(FindRepoFile("mvc-dotnet/mvc/Views/Home/Index.cshtml"));
        Assert.Contains("Atlanta, GA", view);
        Assert.Contains("New York, NY", view);
        Assert.Contains("Toronto, ON", view);
        Assert.Contains("Charlotte, NC", view);
        Assert.Contains("new Guid(\"", view);
        Assert.Contains("59e2459a-b25d-44a7-bcb0-2a4f2e444272", view);
        Assert.DoesNotContain("id = \"nyc\"", view);
        Assert.DoesNotContain("id = \"atlanta\"", view);
    }

    [Fact]
    public void WeatherMapScript_ShowsHoverCardWithGetCurrentAiWeatherButton()
    {
        var script = File.ReadAllText(FindRepoFile("mvc-dotnet/mvc/wwwroot/js/weatherMap.js"));
        Assert.Contains("currentAiWeatherPath", script);
        Assert.Contains("formatLocationWithLatLong", script);
        Assert.Contains("formatHemisphereDegrees", script);
        Assert.Contains("city.lat", script);
        Assert.Contains("city.lng", script);
        Assert.Contains("encodeURIComponent", script);
        Assert.Contains("/current-ai-weather?location=", script);
        Assert.Contains("marker.addListener('mouseover'", script);
        Assert.Contains("marker.addListener('click', openCard)", script);
        Assert.Contains("Get Current AI Weather", script);
        Assert.Contains("bindPinHoverCard", script);
        Assert.Contains("weather-map-pin-card-delete", script);
        Assert.Contains("addCity", script);
        Assert.Contains("removeCity", script);
        Assert.Contains("LIGHT_MAP_STYLES", script);
        Assert.Contains("weather-theme-change", script);
        Assert.Contains("colorScheme", script);
        Assert.Contains("RenderingType.RASTER", script);
        Assert.Contains("createThemedMap", script);
        Assert.Contains("59e2459a-b25d-44a7-bcb0-2a4f2e444272", script);
        Assert.DoesNotContain("id: 'nyc'", script);
    }

    [Fact]
    public void CurrentAIWeatherScript_ConsumesLocationQueryAndClearsUrl()
    {
        var script = File.ReadAllText(FindRepoFile("mvc-dotnet/mvc/wwwroot/js/currentAIWeather.js"));
        Assert.Contains("consumeLocationQuery", script);
        Assert.Contains("params.get('location')", script);
        Assert.Contains("history.replaceState", script);
        Assert.Contains("requestWeather()", script);
    }

    [Fact]
    public async Task ChatClients_ReturnsOkAtCanonicalRoute()
    {
        var response = await _client.GetAsync("/chat-clients");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Chat Clients", html);
        Assert.Contains("In-process · Responses API · Like Foundry Console V3", html);
        Assert.Contains("Remote MCP · Agent Framework · Like Foundry Console V4", html);
        Assert.Contains("class=\"page-shell\"", html);
        Assert.Contains("class=\"chat-tab is-active\"", html);
        Assert.Contains("aria-label=\"Enter fullscreen\"", html);
        Assert.Contains("chatFullscreen.js", html);
        Assert.Contains("chatMarkdown.js", html);
        Assert.Contains("marked.min.js", html);
        Assert.Contains("purify.min.js", html);
        Assert.DoesNotContain("aria-label=\"Add location\"", html);
        Assert.DoesNotContain("Hello World</h2>", html);
        Assert.DoesNotContain("Current AI Weather</h2>", html);
        Assert.DoesNotContain("id=\"weather-map\"", html);

        var script = File.ReadAllText(FindRepoFile("mvc-dotnet/mvc/wwwroot/js/chatClient.js"));
        Assert.Contains("function scrollToBottom()", script);
        Assert.Contains("function requestScrollToBottom(tabId)", script);
        Assert.Contains("payload.type === 'done'", script);
        Assert.Contains("requestScrollToBottom(tabId)", script);
        Assert.Contains("function formatToolHoverText(entry)", script);
        Assert.Contains("dataset.toolDetails", script);
        Assert.Contains("payload.toolArguments", script);
        Assert.Contains("payload.toolResult", script);
        Assert.Contains("chat-tool-hover-card", script);
        Assert.Contains("chat-tool-hover-wrap", script);
        Assert.Contains("scheduleToolHoverHide", script);
        Assert.Contains("TOOL_HOVER_CLOSE_DELAY_MS", script);
        Assert.Contains("chatMarkdown.render", script);
        Assert.Contains("streaming", script);

        var markdown = File.ReadAllText(FindRepoFile("mvc-dotnet/mvc/wwwroot/js/chatMarkdown.js"));
        Assert.Contains("marked.parse", markdown);
        Assert.Contains("gfm: true", markdown);
        Assert.Contains("DOMPurify.sanitize", markdown);

        var fullscreen = File.ReadAllText(FindRepoFile("mvc-dotnet/mvc/wwwroot/js/chatFullscreen.js"));
        Assert.Contains("data-chat-fullscreen-button", fullscreen);
        Assert.Contains("requestFullscreen", fullscreen);
        Assert.Contains("is-css-fullscreen", fullscreen);
    }

    [Fact]
    public async Task Layout_UsesHandWrittenCssAndSemanticClasses()
    {
        var response = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("css/site.css", html);
        Assert.Contains("class=\"header-actions\"", html);
        Assert.Contains("aria-label=\"Add location\"", html);
        Assert.Contains("addLocation.js", html);
        Assert.Contains("data-geo-url", html);
        Assert.Contains("class=\"site-header\"", html);
        Assert.Contains("class=\"avatar-menu\"", html);
        Assert.Contains("class=\"site-main\"", html);
        Assert.Contains("avatar.svg", html);
        Assert.Contains("class=\"avatar-icon\"", html);
        Assert.DoesNotContain("bg-blue-800", html);
        Assert.DoesNotContain("flex-1", html);
        Assert.DoesNotContain("rounded-xl", html);

        var layoutSource = File.ReadAllText(FindRepoFile("mvc-dotnet/mvc/Views/Shared/_Layout.cshtml"));
        Assert.Contains("avatar.svg", layoutSource);
        Assert.Contains("isMapPage", layoutSource);
        Assert.DoesNotContain("stroke-width=\"2.25\"", layoutSource);

        var avatarSvg = File.ReadAllText(FindRepoFile("mvc-dotnet/mvc/wwwroot/avatar.svg"));
        Assert.Contains("<path ", avatarSvg);
        Assert.DoesNotContain("<circle ", avatarSvg);
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
        Assert.Contains(".weather-map-pin-card", css);
        Assert.Contains(".weather-map-pin-card-delete", css);
        Assert.Contains(".add-location-panel", css);
        Assert.Contains(".chat-tool-hover-card", css);
        Assert.Contains(".chat-tool-hover-wrap", css);
        Assert.Contains(".chat-markdown", css);
        Assert.Contains(".chat-fullscreen-button", css);
        Assert.Contains("html.dark", css);
        Assert.Contains("color-scheme: light", css);
        Assert.Contains("html[data-theme=\"dark\"] #weather-map", css);
        Assert.Contains("[hidden]", css);
        Assert.DoesNotContain("tailwindcss", css, StringComparison.OrdinalIgnoreCase);
    }
}
