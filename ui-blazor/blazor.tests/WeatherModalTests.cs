using System.Net;
using System.Net.Http.Json;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.FluentUI.AspNetCore.Components;
using WeatherBlazor.Data;

namespace WeatherBlazor.Tests;

public sealed class WeatherModalTests
{
    [Fact]
    public void RendersModalTitleTabsAndWiresCurrentAIWeatherTab()
    {
        using var context = CreateContext();
        context.Services.GetRequiredService<NavigationManager>()
            .NavigateTo("/weather?name=Atlanta%2C%20GA&lat=33.749&lng=-84.388&tab=current");

        var rendered = context.Render<WeatherBlazor.Pages.Weather>();

        Assert.Contains("weather-dialog-title", rendered.Markup);
        Assert.Contains("Atlanta, GA (33.7490° N, 84.3880° W)", rendered.Markup);
        Assert.Contains("Daily Forecast", rendered.Markup);
        Assert.Contains("Hourly Forecast", rendered.Markup);
        Assert.Contains("Every 15 Forecast", rendered.Markup);
        Assert.Contains("Daily History", rendered.Markup);
        Assert.Contains("Hourly History", rendered.Markup);
        Assert.Contains("aria-modal=\"true\"", rendered.Markup);

        rendered.WaitForAssertion(() =>
        {
            Assert.Contains("current-ai-weather-modal-heading", rendered.Markup);
            Assert.Contains("<strong>Sunny</strong>", rendered.Markup);
            Assert.Contains("72 °F", rendered.Markup);
        });
    }

    [Fact]
    public void NonCurrentTabShowsComingSoonPlaceholder()
    {
        using var context = CreateContext();
        context.Services.GetRequiredService<NavigationManager>()
            .NavigateTo("/weather?name=Atlanta&lat=33.749&lng=-84.388&tab=daily-forecast");

        var rendered = context.Render<WeatherBlazor.Pages.Weather>();

        Assert.Contains("Coming soon.", rendered.Markup);
        Assert.Contains("coming-soon-tab", rendered.Markup);
        Assert.DoesNotContain("current-ai-weather-modal-heading", rendered.Markup);
    }

    [Fact]
    public void CloseButtonNavigatesBackToTheMap()
    {
        using var context = CreateContext();
        var navigation = context.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/weather?name=Atlanta&lat=33.749&lng=-84.388&tab=current");

        var rendered = context.Render<WeatherBlazor.Pages.Weather>();
        rendered.Find("button.about-close").Click();

        Assert.Equal("http://localhost/", navigation.Uri);
    }

    private static BunitContext CreateContext()
    {
        var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddHttpClient();
        context.Services.AddFluentUIComponents();

        var http = new HttpClient(new StubWeatherHandler())
        {
            BaseAddress = new Uri("http://localhost/"),
        };
        context.Services.AddSingleton(new WeatherApiClient(http, NullLogger<WeatherApiClient>.Instance));
        return context;
    }

    private sealed class StubWeatherHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            if (path.Contains("AIWeather/Current", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new AIWeatherResponse
                    {
                        FullSummary = "**Sunny** in Atlanta.",
                        TemperatureF = 72,
                        WindSpeedMPH = 5,
                        WindDirection = "S",
                        WindDirectionDegrees = 180,
                        Conditions = "Clear",
                        Latitude = 33.749,
                        Longitude = -84.388,
                    }),
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
