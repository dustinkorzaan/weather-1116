using System.Net;
using System.Net.Http.Json;
using System.Text;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.FluentUI.AspNetCore.Components;
using WeatherBlazor.Data;
using WeatherBlazor.Shared;

namespace WeatherBlazor.Tests;

public sealed class PageSplitTests
{
    [Fact]
    public void Index_RendersMapWithoutSplitPageContent()
    {
        using var context = CreateContext();
        var rendered = context.Render<WeatherBlazor.Pages.Index>();

        Assert.Contains("id=\"weather-map\"", rendered.Markup);
        Assert.Contains("Atlanta, GA", rendered.Markup);
        Assert.Contains("New York, NY", rendered.Markup);
        Assert.Contains("59e2459a-b25d-44a7-bcb0-2a4f2e444272", rendered.Markup);
        Assert.DoesNotContain("\"nyc\"", rendered.Markup);
        Assert.DoesNotContain("Chat Clients", rendered.Markup);
        Assert.DoesNotContain("<h2 class=\"section-title\">Current AI Weather</h2>", rendered.Markup);
        Assert.DoesNotContain("Hello World", rendered.Markup);
        Assert.DoesNotContain("Loading hello message", rendered.Markup);
        Assert.Contains("data-get-location-url=\"/Geo/GetLocation\"", rendered.Markup);
    }

    [Fact]
    public void WeatherMapScript_ShowsDeleteControlAndMutatesPinList()
    {
        var script = File.ReadAllText(FindRepoFile("ui-blazor/blazor/wwwroot/js/weatherMap.js"));
        Assert.Contains("weatherModalPath", script);
        Assert.Contains("weather-map-pin-card-delete", script);
        Assert.Contains("addCity", script);
        Assert.Contains("removeCity", script);
        Assert.Contains("rightclick", script);
        Assert.Contains("Add Location", script);
        Assert.Contains("/Geo/GetLocation", script);
        Assert.Contains("weather-map-cities", script);
        Assert.Contains("59e2459a-b25d-44a7-bcb0-2a4f2e444272", script);
        Assert.DoesNotContain("id: 'nyc'", script);
    }

    [Fact]
    public void HelloWorld_RendersHelloWithoutWeatherChatOrMap()
    {
        using var context = CreateContext();
        var rendered = context.Render<WeatherBlazor.Pages.HelloWorld>();

        rendered.WaitForAssertion(() =>
        {
            Assert.Contains("Hello from test API.", rendered.Markup);
        });

        Assert.Contains("Hello World", rendered.Markup);
        Assert.DoesNotContain("Chat Clients", rendered.Markup);
        Assert.DoesNotContain("Current AI Weather", rendered.Markup);
        Assert.DoesNotContain("chat-input", rendered.Markup);
        Assert.DoesNotContain("id=\"weather-map\"", rendered.Markup);
    }

    [Fact]
    public void CurrentAIWeather_RendersWeatherFormWithoutHelloChatOrMap()
    {
        using var context = CreateContext();
        var rendered = context.Render<WeatherBlazor.Pages.CurrentAIWeather>();

        Assert.Contains("Current AI Weather", rendered.Markup);
        Assert.Contains("Get Current AI Weather", rendered.Markup);

        var pageSource = File.ReadAllText(FindRepoFile("ui-blazor/blazor/Pages/CurrentAIWeather.razor"));
        Assert.Contains("Class=\"ai-weather-submit\"", pageSource);
        Assert.Contains("Slot=\"start\"", pageSource);
        Assert.Contains("chat-markdown", pageSource);
        Assert.Contains("SafeGfmMarkdown.ToHtml", pageSource);
        Assert.Contains("MarkupString", pageSource);
        Assert.Contains("OnAfterRenderAsync", pageSource);
        Assert.Contains("stat-label\">Temperature<", pageSource);
        Assert.Contains("stat-label\">Wind Speed<", pageSource);
        Assert.Contains("stat-label\">Lat/Long<", pageSource);
        Assert.Contains("FormatTemperatureF", pageSource);
        Assert.Contains("FormatWindSpeedMph", pageSource);
        Assert.Contains("FormatWindDirection", pageSource);
        Assert.Contains("FormatLatLong", pageSource);
        Assert.Contains("wind-direction-arrow", pageSource);
        Assert.True(
            pageSource.IndexOf("@FormatWindDirection", StringComparison.Ordinal)
                < pageSource.IndexOf("wind-direction-arrow", StringComparison.Ordinal),
            "Wind direction arrow should follow the compass label.");
        Assert.Contains("&#x27A4;", pageSource);
        Assert.Contains("WindDirectionDegrees + 90", pageSource);
        Assert.DoesNotContain("Temperature F", pageSource);
        Assert.DoesNotContain("Wind Speed MPH", pageSource);
        Assert.DoesNotContain("protected override async Task OnParametersSetAsync", pageSource);
        Assert.DoesNotContain("Hello World", rendered.Markup);
        Assert.DoesNotContain("Chat Clients", rendered.Markup);
        Assert.DoesNotContain("chat-input", rendered.Markup);
        Assert.DoesNotContain("id=\"weather-map\"", rendered.Markup);
    }

    [Fact]
    public void CurrentAIWeather_LoadingPutsSpinnerInStartSlotBesideTheLabel()
    {
        using var context = CreateContext(holdWeather: true);
        context.Services.GetRequiredService<NavigationManager>()
            .NavigateTo("/current-ai-weather?location=nashville%20tn");

        var rendered = context.Render<WeatherBlazor.Pages.CurrentAIWeather>();

        rendered.WaitForAssertion(() =>
        {
            Assert.Contains("Get Current AI Weather", rendered.Markup);
            Assert.Contains("ai-weather-submit", rendered.Markup);
            Assert.Contains("fluent-progress-ring", rendered.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("slot=\"start\"", rendered.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void CurrentAIWeather_LocationQuery_FillsSearchClearsUrlAndFetches()
    {
        using var context = CreateContext();
        context.Services.GetRequiredService<NavigationManager>()
            .NavigateTo("/current-ai-weather?location=nashville%20tn");

        var rendered = context.Render<WeatherBlazor.Pages.CurrentAIWeather>();

        rendered.WaitForAssertion(() =>
        {
            Assert.Contains("chat-markdown", rendered.Markup);
            Assert.Contains("<strong>Sunny</strong>", rendered.Markup);
            Assert.Contains("in Nashville.", rendered.Markup);
            Assert.DoesNotContain("**Sunny**", rendered.Markup);
            Assert.Contains("Temperature", rendered.Markup);
            Assert.Contains("72 °F", rendered.Markup);
            Assert.Contains("Wind Speed", rendered.Markup);
            Assert.Contains("5 mph", rendered.Markup);
            Assert.Contains("S (180°)", rendered.Markup);
            Assert.Contains("wind-direction-arrow", rendered.Markup);
            Assert.Contains("rotate(90deg)", rendered.Markup);
            Assert.Contains("\u27A4", rendered.Markup);
            Assert.True(
                rendered.Markup.IndexOf("S (180°)", StringComparison.Ordinal)
                    < rendered.Markup.IndexOf("wind-direction-arrow", StringComparison.Ordinal),
                "Wind direction arrow should follow the compass label.");
            Assert.Contains("Lat/Long", rendered.Markup);
            Assert.Contains("36.16° N, 86.78° W", rendered.Markup);
            Assert.DoesNotContain("Temperature F", rendered.Markup);
            Assert.DoesNotContain("Wind Speed MPH", rendered.Markup);
        });

        Assert.Contains("nashville tn", rendered.Markup);
        Assert.DoesNotContain("location=nashville", context.Services.GetRequiredService<NavigationManager>().Uri);
    }

    [Fact]
    public void WeatherMapScript_ShowsHoverCardWithWeatherButton()
    {
        var script = File.ReadAllText(FindRepoFile("ui-blazor/blazor/wwwroot/js/weatherMap.js"));
        Assert.Contains("weatherModalPath", script);
        Assert.Contains("navigateToWeather", script);
        Assert.Contains("window.Blazor.navigateTo", script);
        Assert.Contains("city.lat", script);
        Assert.Contains("city.lng", script);
        Assert.Contains("/weather?", script);
        Assert.Contains("marker.addListener('mouseover'", script);
        Assert.Contains("marker.addListener('click', openCard)", script);
        Assert.Contains("SEARCH_ICON_SVG", script);
        Assert.Contains("bindPinHoverCard", script);
        Assert.Contains("bindRightClickAddLocation", script);
        Assert.Contains("Add Location", script);
        Assert.Contains("mapTypeControl: true", script);
        Assert.Contains("SATELLITE", script);
        Assert.Contains("ROADMAP", script);
        Assert.Contains("HYBRID", script);
        Assert.Contains("MapTypeId.HYBRID", script);
        Assert.Contains("LIGHT_MAP_STYLES", script);
        Assert.Contains("weather-theme-change", script);
        Assert.Contains("colorScheme", script);
        Assert.Contains("RenderingType.RASTER", script);
        Assert.Contains("createThemedMap", script);
    }

    [Fact]
    public void ChatClients_RendersChatWithoutHelloWeatherOrMap()
    {
        using var context = CreateContext();
        var rendered = context.Render<WeatherBlazor.Pages.ChatClients>();

        Assert.Contains("Chat Clients", rendered.Markup);
        Assert.Contains("In-process · Responses API · Like Foundry Console V3", rendered.Markup);
        Assert.Contains("chat-input", rendered.Markup);
        Assert.Contains("Enter fullscreen", rendered.Markup);
        Assert.DoesNotContain("Hello World", rendered.Markup);
        Assert.DoesNotContain("Current AI Weather", rendered.Markup);
        Assert.DoesNotContain("Loading hello message", rendered.Markup);
        Assert.DoesNotContain("id=\"weather-map\"", rendered.Markup);

        var panelSource = File.ReadAllText(FindRepoFile("ui-blazor/blazor/Shared/ChatPanel.razor"));
        Assert.Contains("Hosted Foundry agent · Like Foundry Console V5", panelSource);
        Assert.Contains("chatInput.scrollToBottom", panelSource);
        Assert.Contains("chatInput.getValue", panelSource);
        Assert.Contains("chatInput.setValue", panelSource);
        Assert.Contains("textarea", panelSource);
        Assert.DoesNotContain("Immediate=\"true\"", panelSource);
        Assert.DoesNotContain("@bind-Value=\"_input\"", panelSource);
        Assert.DoesNotContain("@oninput", panelSource);
        Assert.Contains("Type == \"done\"", panelSource);
        Assert.Contains("RequestScrollToBottom", panelSource);
        Assert.Contains("data-tool-details", panelSource);
        Assert.Contains("ToolHoverAttributes", panelSource);
        Assert.Contains("ToolArguments", panelSource);
        Assert.Contains("ToolResult", panelSource);
        Assert.Contains("SafeGfmMarkdown.ToHtml", panelSource);
        Assert.Contains("MarkupString", panelSource);
        Assert.Contains("Streaming", panelSource);
        Assert.Contains("chat-fullscreen-button", panelSource);
        Assert.Contains("Enter fullscreen", panelSource);
        Assert.Contains("chat-window", panelSource);

        var markdown = File.ReadAllText(FindRepoFile("ui-blazor/blazor/Markdown/SafeGfmMarkdown.cs"));
        Assert.Contains("UsePipeTables", markdown);
        Assert.Contains("UseEmphasisExtras", markdown);
        Assert.Contains("UseAutoLinks", markdown);
        Assert.Contains("UseTaskLists", markdown);
        Assert.DoesNotContain("UseAdvancedExtensions", markdown);
        Assert.DoesNotContain("UseMediaLinks", markdown);
        Assert.Contains("Markdig.Markdown.ToHtml", markdown);
        Assert.Contains("DisableHtml", markdown);
        Assert.Contains("IsSafeUrl", markdown);

        var chatInput = File.ReadAllText(FindRepoFile("ui-blazor/blazor/wwwroot/js/chatInput.js"));
        Assert.Contains("function scrollToBottom(element)", chatInput);
        Assert.Contains("element.scrollTop = element.scrollHeight", chatInput);
        Assert.Contains("function getValue(element)", chatInput);
        Assert.Contains("function setValue(element, value)", chatInput);
        Assert.Contains("data-tool-details", chatInput);
        Assert.Contains("chat-tool-hover-card", chatInput);
        Assert.Contains("chat-tool-hover-wrap", chatInput);
        Assert.Contains("scheduleHide", chatInput);
        Assert.Contains("TOOL_HOVER_CLOSE_DELAY_MS", chatInput);

        var fullscreen = File.ReadAllText(FindRepoFile("ui-blazor/blazor/wwwroot/js/chatFullscreen.js"));
        Assert.Contains("data-chat-fullscreen-button", fullscreen);
        Assert.Contains("requestFullscreen", fullscreen);
        Assert.Contains("is-css-fullscreen", fullscreen);
    }

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

    private static BunitContext CreateContext(bool holdWeather = false)
    {
        var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddHttpClient();
        context.Services.AddFluentUIComponents();
        context.Services.AddSingleton<IConfiguration>(
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["GOOGLE_MAPS_API_KEY"] = "",
                    ["API_DOTNET_URL"] = "http://localhost:8080",
                })
                .Build());

        var http = new HttpClient(new StubHelloHandler(holdWeather))
        {
            BaseAddress = new Uri("http://localhost/"),
        };
        context.Services.AddSingleton(new WeatherApiClient(http, NullLogger<WeatherApiClient>.Instance));
        context.Services.AddSingleton(new ChatApiClient(http));
        return context;
    }

    private sealed class StubHelloHandler(bool holdWeather) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            if (path.EndsWith("/Home/Hello", StringComparison.OrdinalIgnoreCase)
                || path.Equals("/Home/Hello", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith("Home/Hello", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new HelloWorldResponse
                    {
                        RequestMessage = "from test",
                        RequestResponse = "Hello from test API.",
                    }),
                };
            }

            if (path.Contains("AIWeather/Current", StringComparison.OrdinalIgnoreCase)
                || path.Contains("/Geo", StringComparison.OrdinalIgnoreCase)
                || path.Equals("/Geo", StringComparison.OrdinalIgnoreCase))
            {
                if (holdWeather)
                {
                    await Task.Delay(Timeout.Infinite, cancellationToken);
                }

                if (path.Contains("Geo/GetLocation", StringComparison.OrdinalIgnoreCase))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = JsonContent.Create(new LocationResponse
                        {
                            Location = "Nashville, Tennessee",
                        }),
                    };
                }

                if (path.Contains("/Geo", StringComparison.OrdinalIgnoreCase)
                    || path.Equals("/Geo", StringComparison.OrdinalIgnoreCase))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = JsonContent.Create(new LatLongResponse
                        {
                            Rank = 1,
                            Name = "Nashville",
                            State = "Tennessee",
                            Country = "United States",
                            Latitude = 36.1627,
                            Longitude = -86.7816,
                        }),
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new AIWeatherResponse
                    {
                        FullSummary = "**Sunny** in Nashville.",
                        TemperatureF = 72,
                        WindSpeedMPH = 5,
                        WindDirection = "S",
                        WindDirectionDegrees = 180,
                        Conditions = "Clear",
                        Latitude = 36.1627,
                        Longitude = -86.7816,
                    }),
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json"),
            };
        }
    }
}
