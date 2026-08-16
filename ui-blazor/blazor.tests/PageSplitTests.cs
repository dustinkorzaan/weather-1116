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
        Assert.Contains("Get Current AI Weather", rendered.Markup);
        Assert.Contains("weather-map-pin-card-preview", rendered.Markup);
        Assert.Contains("weather-map-pin-card-delete", rendered.Markup);
        Assert.Contains("weather-map-pin-card-header", rendered.Markup);
    }

    [Fact]
    public void WeatherMapScript_ShowsDeleteControlAndMutatesPinList()
    {
        var script = File.ReadAllText(FindRepoFile("ui-blazor/blazor/wwwroot/js/weatherMap.js"));
        Assert.Contains("currentAiWeatherPath", script);
        Assert.Contains("Get Current AI Weather", script);
        Assert.Contains("weather-map-pin-card-delete", script);
        Assert.Contains("addCity", script);
        Assert.Contains("removeCity", script);
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
            Assert.Contains("Sunny in Nashville.", rendered.Markup);
        });

        Assert.Contains("nashville tn", rendered.Markup);
        Assert.DoesNotContain("location=nashville", context.Services.GetRequiredService<NavigationManager>().Uri);
    }

    [Fact]
    public void WeatherMapScript_ShowsHoverCardWithGetCurrentAiWeatherButton()
    {
        var script = File.ReadAllText(FindRepoFile("ui-blazor/blazor/wwwroot/js/weatherMap.js"));
        Assert.Contains("currentAiWeatherPath", script);
        Assert.Contains("encodeURIComponent", script);
        Assert.Contains("/current-ai-weather?location=", script);
        Assert.Contains("marker.addListener('mouseover'", script);
        Assert.Contains("marker.addListener('click', openCard)", script);
        Assert.Contains("Get Current AI Weather", script);
        Assert.Contains("bindPinHoverCard", script);
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
        Assert.DoesNotContain("Hello World", rendered.Markup);
        Assert.DoesNotContain("Current AI Weather", rendered.Markup);
        Assert.DoesNotContain("Loading hello message", rendered.Markup);
        Assert.DoesNotContain("id=\"weather-map\"", rendered.Markup);

        var panelSource = File.ReadAllText(FindRepoFile("ui-blazor/blazor/Shared/ChatPanel.razor"));
        Assert.Contains("chatInput.scrollToBottom", panelSource);
        Assert.Contains("Type == \"done\"", panelSource);
        Assert.Contains("RequestScrollToBottom", panelSource);

        var chatInput = File.ReadAllText(FindRepoFile("ui-blazor/blazor/wwwroot/js/chatInput.js"));
        Assert.Contains("function scrollToBottom(element)", chatInput);
        Assert.Contains("element.scrollTop = element.scrollHeight", chatInput);
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
                        FullSummary = "Sunny in Nashville.",
                        TemperatureF = 72,
                        WindSpeedMPH = 5,
                        WindDirection = "S",
                        Conditions = "Clear",
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
