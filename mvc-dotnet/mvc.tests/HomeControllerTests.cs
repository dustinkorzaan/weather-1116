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
        Assert.Contains("chat-markdown", html);
        Assert.Contains("safeGfmMarkdown.js", html);
        Assert.Contains("marked.min.js", html);
        Assert.Contains("purify.min.js", html);
        Assert.Contains("<dt>Temperature</dt>", html);
        Assert.Contains("<dt>Wind Speed</dt>", html);
        Assert.Contains("<dt>Wind Direction</dt>", html);
        Assert.Contains("class=\"wind-direction\"", html);
        Assert.Contains("<dt>Lat/Long</dt>", html);
        Assert.Contains("id=\"ai-weather-lat-long\"", html);
        Assert.DoesNotContain("Temperature F", html);
        Assert.DoesNotContain("Wind Speed MPH", html);
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
        Assert.Contains("windDirectionDisplay.js", html);
        Assert.Contains("currentAIWeather.js", html);
    }

    [Fact]
    public async Task CurrentAIWeather_HasV3V4V5Tabs()
    {
        var response = await _client.GetAsync("/current-ai-weather");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("data-ai-weather-tab=\"v3\"", html);
        Assert.Contains("data-ai-weather-tab=\"v4\"", html);
        Assert.Contains("data-ai-weather-tab=\"v5\"", html);
        Assert.Contains("In-process tool loop · Like Foundry Console V3", html);
        Assert.Contains("Remote MCP tools · Like Foundry Console V4", html);
        Assert.Contains("Hosted Foundry agent · Like Foundry Console V5", html);
        Assert.Contains("id=\"ai-weather-form-v4\"", html);
        Assert.Contains("id=\"ai-weather-location-v4\"", html);
        Assert.Contains("id=\"ai-weather-form-v5\"", html);
        Assert.Contains("id=\"ai-weather-location-v5\"", html);
        Assert.Contains("currentAIWeatherTabs.js", html);
        Assert.Contains("GetCurrentAIWeatherV3", html);
        Assert.Contains("GetCurrentAIWeatherV4", html);
        Assert.Contains("GetCurrentAIWeatherV5", html);
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
        Assert.Contains("data-get-location-url", view);
        Assert.Contains("GetLocation", view);
    }

    [Fact]
    public void WeatherMapScript_ShowsHoverCardWithWeatherButton()
    {
        var script = File.ReadAllText(FindRepoFile("mvc-dotnet/mvc/wwwroot/js/weatherMap.js"));
        Assert.Contains("weatherModalPath", script);
        Assert.Contains("formatLocationWithLatLong", script);
        Assert.Contains("formatHemisphereDegrees", script);
        Assert.Contains("city.lat", script);
        Assert.Contains("city.lng", script);
        Assert.Contains("encodeURIComponent", script);
        Assert.Contains("'/weather?'", script);
        Assert.Contains("marker.addListener('mouseover'", script);
        Assert.Contains("marker.addListener('click', openCard)", script);
        Assert.Contains(">Weather</span>", script);
        Assert.Contains("bindPinHoverCard", script);
        Assert.Contains("bindRightClickAddLocation", script);
        Assert.Contains("Add Location", script);
        Assert.Contains("mapTypeControl: true", script);
        Assert.Contains("SATELLITE", script);
        Assert.Contains("ROADMAP", script);
        Assert.Contains("HYBRID", script);
        Assert.Contains("MapTypeId.HYBRID", script);
        Assert.Contains("weather-map-pin-card-delete", script);
        Assert.Contains("addCity", script);
        Assert.Contains("removeCity", script);
        Assert.Contains("rightclick", script);
        Assert.Contains("Add Location", script);
        Assert.Contains("/Geo/GetLocation", script);
        Assert.Contains("LIGHT_MAP_STYLES", script);
        Assert.Contains("weather-theme-change", script);
        Assert.Contains("colorScheme", script);
        Assert.Contains("RenderingType.RASTER", script);
        Assert.Contains("createThemedMap", script);
        Assert.Contains("59e2459a-b25d-44a7-bcb0-2a4f2e444272", script);
        Assert.DoesNotContain("id: 'nyc'", script);
    }

    [Fact]
    public void WindDirectionDisplayScript_ExportsSharedWindHelpers()
    {
        var script = File.ReadAllText(FindRepoFile("mvc-dotnet/mvc/wwwroot/js/windDirectionDisplay.js"));
        Assert.Contains("window.windDirectionDisplay", script);
        Assert.DoesNotContain("windArrowRotationDeg", script);
        Assert.DoesNotContain("degreesToCompass", script);
        Assert.Contains("normalizeSourceDegrees", script);
        Assert.Contains("Math.round(((numeric % 360) + 360) % 360)", script);
        Assert.Contains("renderWindDirection", script);
        Assert.Contains("createWindDirectionCell", script);
        Assert.Contains("WIND_DIRECTION_ARROW = 'v'", script);
        Assert.DoesNotContain("numeric + 180", script);
    }

    [Fact]
    public void CurrentAIWeatherScript_ConsumesLocationQueryAndClearsUrl()
    {
        var script = File.ReadAllText(FindRepoFile("mvc-dotnet/mvc/wwwroot/js/currentAIWeather.js"));
        Assert.Contains("consumeLocationQuery", script);
        Assert.Contains("params.get('location')", script);
        Assert.Contains("history.replaceState", script);
        Assert.Contains("requestWeather()", script);
        Assert.Contains("safeGfmMarkdown.render", script);
        Assert.Contains("summaryEl.innerHTML", script);
        Assert.Contains("formatTemperatureF", script);
        Assert.Contains("formatWindSpeedMph", script);
        Assert.Contains("formatLatLong", script);
        Assert.Contains("windDirectionDisplay.renderWindDirection", script);
        Assert.DoesNotContain("function windArrowRotationDeg", script);
        Assert.DoesNotContain("numeric + 180", script);
        Assert.DoesNotContain("Math.round(numeric) - 90", script);
        Assert.Contains("toFixed(2)", script);
        Assert.Contains("windDirectionSourceDegrees", script);
        Assert.Contains("data.latitude", script);
        Assert.Contains("data.longitude", script);
    }

    [Fact]
    public void WeatherModalGridsScript_RendersRotatedWindArrow()
    {
        var script = File.ReadAllText(FindRepoFile("mvc-dotnet/mvc/wwwroot/js/weatherModalGrids.js"));
        Assert.Contains("windDirectionDisplay.createWindDirectionCell", script);
        Assert.DoesNotContain("function windArrowRotationDeg", script);
        Assert.DoesNotContain("numeric + 180", script);
        Assert.DoesNotContain("Math.round(numeric) - 90", script);
    }

    [Fact]
    public async Task Weather_ReturnsOkWithTabsAndCurrentAIWeatherWiredUp()
    {
        var response = await _client.GetAsync("/weather?name=Nashville%2C%20TN&lat=36.1627&lng=-86.7816&tab=current");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Nashville, TN (36.1627", html);
        Assert.Contains("N, 86.7816", html);
        Assert.Contains("W)</h1>", html);
        Assert.Contains("class=\"weather-modal-tab is-active\"", html);
        Assert.Contains("Current AI Weather</h2>", html);
        Assert.Contains("id=\"weatherModalRefresh\"", html);
        Assert.Contains("weatherModal.js", html);
        Assert.Contains("safeGfmMarkdown.js", html);
        Assert.Contains("Daily Forecast", html);
        Assert.Contains("Hourly Forecast", html);
        Assert.Contains("Every 15 Forecast", html);
        Assert.Contains("Daily History", html);
        Assert.Contains("Hourly History", html);
        Assert.Contains("tab=daily-forecast", html);
        Assert.DoesNotContain("Coming soon.", html);
    }

    [Fact]
    public async Task Weather_WiresDailyForecastTabToForecastGrid()
    {
        var response = await _client.GetAsync("/weather?name=Nashville%2C%20TN&lat=36.1627&lng=-86.7816&tab=daily-forecast");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Daily Forecast</h2>", html);
        Assert.Contains("id=\"weatherModalGridRefresh\"", html);
        Assert.Contains("id=\"weatherModalGridBody\"", html);
        Assert.Contains("<th>Date</th>", html);
        Assert.Contains("<th>High</th>", html);
        Assert.Contains("<th>Low</th>", html);
        Assert.Contains("<th>Wind Direction</th>", html);
        Assert.Contains("weatherModalGrids.js", html);
        Assert.Contains("endpoint: \"/Forecast\"", html);
        Assert.Contains("resolution: \"Daily\"", html);
        Assert.Contains("field: \"daily\"", html);
        Assert.Contains("reverse: false", html);
        Assert.DoesNotContain("weatherModal.js", html);
        Assert.DoesNotContain("Coming soon.", html);
    }

    [Fact]
    public async Task Weather_WiresHourlyHistoryTabToHistoryGridReversed()
    {
        var response = await _client.GetAsync("/weather?name=Nashville%2C%20TN&lat=36.1627&lng=-86.7816&tab=hourly-history");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Hourly History</h2>", html);
        Assert.Contains("<th>Date</th>", html);
        Assert.Contains("<th>Time</th>", html);
        Assert.Contains("<th>Temp</th>", html);
        Assert.Contains("endpoint: \"/History\"", html);
        Assert.Contains("resolution: \"Hourly\"", html);
        Assert.Contains("field: \"hourly\"", html);
        Assert.Contains("reverse: true", html);
    }

    [Fact]
    public async Task Weather_DefaultsToCurrentTabForUnknownTabValue()
    {
        var response = await _client.GetAsync("/weather?name=Nashville%2C%20TN&tab=bogus");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Current AI Weather</h2>", html);
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
        Assert.Contains("Hosted Foundry agent · Like Foundry Console V5", html);
        Assert.Contains("class=\"page-shell\"", html);
        Assert.Contains("class=\"chat-tab is-active\"", html);
        Assert.Contains("aria-label=\"Enter fullscreen\"", html);
        Assert.Contains("chatFullscreen.js", html);
        Assert.Contains("safeGfmMarkdown.js", html);
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
        Assert.Contains("safeGfmMarkdown.render", script);
        Assert.Contains("streaming", script);

        // The hover card must follow the chat window into (and back out of)
        // native fullscreen, including WebKit's prefixed API, or it renders
        // invisible outside the fullscreen top layer.
        Assert.Contains("document.webkitFullscreenElement", script);
        Assert.Contains("document.addEventListener('fullscreenchange', onToolHoverFullscreenChange)", script);
        Assert.Contains("document.addEventListener('webkitfullscreenchange', onToolHoverFullscreenChange)", script);

        var markdown = File.ReadAllText(FindRepoFile("mvc-dotnet/mvc/wwwroot/js/markdown/safeGfmMarkdown.js"));
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
        Assert.Contains(".weather-map-pin-card-header", css);
        Assert.Contains(".weather-map-pin-card-delete", css);
        Assert.Contains(".weather-map-pin-card-delete svg", css);
        Assert.Contains(".weather-map-add-location-button", css);
        Assert.Contains(".wind-direction-arrow", css);
        Assert.DoesNotContain("font-size: 1.15em", css);
        Assert.Contains(".add-location-panel", css);
        Assert.Contains(".chat-tool-hover-card", css);
        Assert.Contains(".chat-tool-hover-wrap", css);
        Assert.Contains(".chat-markdown", css);
        Assert.Contains(".chat-fullscreen-button", css);
        Assert.Contains("flex: 0 0 auto", css);
        Assert.Contains("height: max-content", css);
        Assert.Contains("min-height: min-content", css);
        Assert.Contains("html.dark", css);
        Assert.Contains("color-scheme: light", css);
        Assert.Contains("html[data-theme=\"dark\"] #weather-map", css);
        Assert.Contains("[hidden]", css);
        Assert.DoesNotContain("tailwindcss", css, StringComparison.OrdinalIgnoreCase);
    }
}
