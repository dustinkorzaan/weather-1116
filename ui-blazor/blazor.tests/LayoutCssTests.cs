using Bunit;
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
    }
}
