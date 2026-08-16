using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.FluentUI.AspNetCore.Components;
using WeatherBlazor.Data;
using WeatherBlazor.Shared;

namespace WeatherBlazor.Tests;

public sealed class LayoutCssTests
{
    [Fact]
    public void SiteCss_StretchesFluentBodyContentSoTheMapHasWidth()
    {
        var cssPath = Path.Combine(AppContext.BaseDirectory, "site.css");
        Assert.True(File.Exists(cssPath), $"Expected copied site.css at {cssPath}");

        var css = File.ReadAllText(cssPath);
        Assert.Contains(".layout.weather-shell > .body-content.weather-body", css);
        Assert.Contains("align-items: stretch", css);
        Assert.Contains("color-scheme: light", css);
        Assert.Contains("html[data-theme=\"dark\"] .weather-map", css);
        Assert.Contains(".chat-tool-hover-card", css);
        Assert.Contains(".chat-tool-hover-wrap", css);
        Assert.Contains(".chat-message.tool", css);
        Assert.Contains(".chat-markdown", css);
        Assert.Contains(".chat-fullscreen-button", css);
        Assert.Contains(".chat-form textarea.chat-input", css);
        Assert.Contains(".weather-map-add-location-button", css);
        Assert.Contains(".weather-map-add-location-error", css);
    }

    [Fact]
    public void MainLayout_RendersFluentLayoutClassesTheMapCssTargets()
    {
        using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddHttpClient();
        context.Services.AddFluentUIComponents();
        context.Services.AddSingleton<IConfiguration>(
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["API_DOTNET_URL"] = "http://localhost:8080",
                    ["UI_REACT_URL"] = "http://localhost:3000",
                    ["MVC_URL"] = "http://localhost:8100",
                    ["WORKER_DOTNET_URL"] = "http://localhost:8130",
                })
                .Build());
        context.Services.AddSingleton(
            new WeatherApiClient(new HttpClient { BaseAddress = new Uri("http://localhost/") }, NullLogger<WeatherApiClient>.Instance));

        var rendered = context.Render<MainLayout>(parameters => parameters.Add(layout => layout.Body, "<div id=\"child\"></div>"));

        Assert.Contains("weather-shell", rendered.Markup);
        Assert.Contains("weather-body", rendered.Markup);
        Assert.Contains("class=\"layout weather-shell\"", rendered.Markup);
        Assert.Contains("body-content weather-body", rendered.Markup);
        Assert.Contains("class=\"brand-title\"", rendered.Markup);
        Assert.Contains("<a href=\"/\" class=\"brand-link\">", rendered.Markup);
        Assert.Contains("<h1 class=\"brand-title\">Weather Blazor</h1>", rendered.Markup);
        Assert.Contains("id=\"user-menu-button\"", rendered.Markup);
        Assert.Contains("aria-label=\"Add location\"", rendered.Markup);
        Assert.Contains("src=\"avatar.svg\"", rendered.Markup);
        Assert.Contains("class=\"avatar-icon\"", rendered.Markup);
        Assert.Contains("class=\"about-modal", rendered.Markup);
        Assert.Contains("class=\"about-close\"", rendered.Markup);
        Assert.Contains("aria-label=\"Close\"", rendered.Markup);

        var layoutSource = File.ReadAllText(FindRepoFile("ui-blazor/blazor/Shared/MainLayout.razor"));
        Assert.Contains("Login/Logout", layoutSource);
        Assert.Contains("Hello World", layoutSource);
        Assert.Contains("Current AI Weather", layoutSource);
        Assert.Contains("Chat Clients", layoutSource);
        Assert.Contains("ThemeItemLabel(\"light\", \"Light\")", layoutSource);
        Assert.Contains("ThemeItemLabel(\"dark\", \"Dark\")", layoutSource);
        Assert.Contains("ThemeItemLabel(\"system\", \"System\")", layoutSource);
        Assert.Contains("NavigateTo(\"/hello-world\")", layoutSource);
        Assert.Contains("NavigateTo(\"/current-ai-weather\")", layoutSource);
        Assert.Contains("NavigateTo(\"/chat-clients\")", layoutSource);
        Assert.Contains("OpenExternalAsync", layoutSource);
        Assert.Contains("avatar.svg", layoutSource);
        Assert.Contains("Add location", layoutSource);
        Assert.Contains("IsMapPage", layoutSource);
        Assert.Contains("weatherMap.addCity", layoutSource);
        Assert.Contains("SearchLocation", layoutSource);
        Assert.DoesNotContain("FluentButton", layoutSource);

        var programSource = File.ReadAllText(FindRepoFile("ui-blazor/blazor/Program.cs"));
        Assert.Contains("MapGet(\"/Geo/GetLocation\"", programSource);
        Assert.Contains("client.GetLocation", programSource);

        var weatherClient = File.ReadAllText(FindRepoFile("ui-blazor/blazor/Data/WeatherApiClient.cs"));
        Assert.Contains("Geo/GetLocation?latitude=", weatherClient);

        var avatarSvg = File.ReadAllText(FindRepoFile("ui-blazor/blazor/wwwroot/avatar.svg"));
        Assert.Contains("<path ", avatarSvg);
        Assert.DoesNotContain("<circle ", avatarSvg);

        var host = File.ReadAllText(FindRepoFile("ui-blazor/blazor/Pages/_Host.cshtml"));
        Assert.Contains("--body-font:", host);
        Assert.Contains("chatFullscreen.js", host);

        var app = File.ReadAllText(FindRepoFile("ui-blazor/blazor/App.razor"));
        Assert.Contains("Selector=\".section-title\"", app);
    }

    [Fact]
    public void MainLayout_ShowsAddLocationOnlyOnTheMapPage()
    {
        using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddHttpClient();
        context.Services.AddFluentUIComponents();
        context.Services.AddSingleton<IConfiguration>(
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["API_DOTNET_URL"] = "http://localhost:8080",
                    ["UI_REACT_URL"] = "http://localhost:3000",
                    ["MVC_URL"] = "http://localhost:8100",
                    ["WORKER_DOTNET_URL"] = "http://localhost:8130",
                })
                .Build());
        context.Services.AddSingleton(
            new WeatherApiClient(new HttpClient { BaseAddress = new Uri("http://localhost/") }, NullLogger<WeatherApiClient>.Instance));

        var rendered = context.Render<MainLayout>(parameters => parameters.Add(layout => layout.Body, "<div id=\"child\"></div>"));

        Assert.Contains("aria-label=\"Add location\"", rendered.Markup);

        context.Services.GetRequiredService<NavigationManager>().NavigateTo("/hello-world");

        rendered.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("aria-label=\"Add location\"", rendered.Markup);
        });
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

    [Fact]
    public void SiteCss_KeepsBrandTitleBoldWithoutUnderline()
    {
        var cssPath = Path.Combine(AppContext.BaseDirectory, "site.css");
        Assert.True(File.Exists(cssPath), $"Expected copied site.css at {cssPath}");

        var css = File.ReadAllText(cssPath);
        Assert.Contains("h1.brand-title", css);
        Assert.Contains("font-size: 1.25rem", css);
        Assert.Contains("font-weight: 600", css);
        Assert.Contains("font-family: var(--wx-font)", css);
        Assert.Contains("--body-font: var(--wx-font)", css);
        Assert.DoesNotContain("font-family: inherit", css);
        Assert.Contains("text-decoration: none", css);
        Assert.Contains("outline: none", css);
        Assert.Contains(".form-row-start", css);
        Assert.Contains("align-items: flex-start", css);
        Assert.Contains(".ai-weather-submit", css);
        Assert.Contains(".wind-direction-arrow", css);
        Assert.Contains("white-space: nowrap", css);
        Assert.Contains("flex: 0 0 auto", css);
        Assert.Contains("border: 2px solid var(--wx-border-strong)", css);
        Assert.Contains("html.dark", css);
        Assert.Contains("--wx-map", css);
        Assert.Contains(".avatar-icon", css);
        Assert.Contains(".add-location-button", css);
        Assert.Contains(".weather-map-pin-card-header", css);
        Assert.Contains(".weather-map-pin-card-delete", css);
        Assert.Contains(".weather-map-pin-card-delete svg", css);
        Assert.Contains(".about-modal.is-open", css);
        Assert.Contains(".about-close", css);
        Assert.Contains(".chat-markdown", css);
        Assert.Contains(".chat-fullscreen-button", css);
        Assert.Contains(".chat-form textarea.chat-input", css);
    }
}
