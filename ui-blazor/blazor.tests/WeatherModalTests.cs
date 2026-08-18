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
            .NavigateTo("/weather?name=Nashville%2C%20TN&lat=36.1659&lng=-86.7844&tab=current");

        var rendered = context.Render<WeatherBlazor.Pages.Weather>();

        Assert.Contains("weather-dialog-title", rendered.Markup);
        Assert.Contains("Nashville, TN (36.1659° N, 86.7844° W)", rendered.Markup);
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
    public void DailyForecastTabRendersGridSoonestFirst()
    {
        using var context = CreateContext();
        context.Services.GetRequiredService<NavigationManager>()
            .NavigateTo("/weather?name=Nashville%2C%20TN&lat=36.1659&lng=-86.7844&tab=daily-forecast");

        var rendered = context.Render<WeatherBlazor.Pages.Weather>();

        Assert.DoesNotContain("current-ai-weather-modal-heading", rendered.Markup);

        rendered.WaitForAssertion(() =>
        {
            Assert.Contains("daily-forecast-heading", rendered.Markup);
            Assert.Contains("High</th>", rendered.Markup);
            Assert.Contains("Wind Direction</th>", rendered.Markup);
            Assert.Contains("88.4 °F", rendered.Markup);
            Assert.Contains("5/16\"", rendered.Markup);
            Assert.Contains("SW (224°)", rendered.Markup);
            Assert.Contains("wind-direction-arrow", rendered.Markup);
            Assert.Contains("rotate(134deg)", rendered.Markup);
            Assert.True(
                rendered.Markup.IndexOf("Wed, Aug 19", StringComparison.Ordinal)
                    < rendered.Markup.IndexOf("Thu, Aug 20", StringComparison.Ordinal),
                "Forecast rows should read soonest-first.");
        });
    }

    [Fact]
    public void DailyHistoryTabRendersGridMostRecentFirst()
    {
        using var context = CreateContext();
        context.Services.GetRequiredService<NavigationManager>()
            .NavigateTo("/weather?name=Nashville%2C%20TN&lat=36.1659&lng=-86.7844&tab=daily-history");

        var rendered = context.Render<WeatherBlazor.Pages.Weather>();

        rendered.WaitForAssertion(() =>
        {
            Assert.Contains("daily-history-heading", rendered.Markup);
            Assert.True(
                rendered.Markup.IndexOf("Thu, Aug 20", StringComparison.Ordinal)
                    < rendered.Markup.IndexOf("Wed, Aug 19", StringComparison.Ordinal),
                "History rows should read most-recent-first.");
        });
    }

    [Fact]
    public void HourlyForecastTabRendersClockTimeGrid()
    {
        using var context = CreateContext();
        context.Services.GetRequiredService<NavigationManager>()
            .NavigateTo("/weather?name=Nashville%2C%20TN&lat=36.1659&lng=-86.7844&tab=hourly-forecast");

        var rendered = context.Render<WeatherBlazor.Pages.Weather>();

        rendered.WaitForAssertion(() =>
        {
            Assert.Contains("hourly-forecast-heading", rendered.Markup);
            Assert.Contains(">Date</th>", rendered.Markup);
            Assert.Contains(">Time</th>", rendered.Markup);
            Assert.Contains("Wed, Aug 19", rendered.Markup);
            Assert.Contains(">2 PM<", rendered.Markup);
            Assert.Contains("86.5 °F", rendered.Markup);
        });
    }

    [Fact]
    public void Every15ForecastTabRendersClockTimeGrid()
    {
        using var context = CreateContext();
        context.Services.GetRequiredService<NavigationManager>()
            .NavigateTo("/weather?name=Nashville%2C%20TN&lat=36.1659&lng=-86.7844&tab=every-15-forecast");

        var rendered = context.Render<WeatherBlazor.Pages.Weather>();

        rendered.WaitForAssertion(() =>
        {
            Assert.Contains("every-15-forecast-heading", rendered.Markup);
            Assert.Contains(">Date</th>", rendered.Markup);
            Assert.Contains(">Time</th>", rendered.Markup);
            Assert.Contains("Wed, Aug 19", rendered.Markup);
            Assert.Contains(">2:15 PM<", rendered.Markup);
        });
    }

    [Fact]
    public void HourlyHistoryTabRendersClockTimeGridMostRecentFirst()
    {
        using var context = CreateContext();
        context.Services.GetRequiredService<NavigationManager>()
            .NavigateTo("/weather?name=Nashville%2C%20TN&lat=36.1659&lng=-86.7844&tab=hourly-history");

        var rendered = context.Render<WeatherBlazor.Pages.Weather>();

        rendered.WaitForAssertion(() =>
        {
            Assert.Contains("hourly-history-heading", rendered.Markup);
            Assert.True(
                rendered.Markup.IndexOf(">4 PM<", StringComparison.Ordinal)
                    < rendered.Markup.IndexOf(">2 PM<", StringComparison.Ordinal),
                "History rows should read most-recent-first.");
        });
    }

    [Fact]
    public void CloseButtonNavigatesBackToTheMap()
    {
        using var context = CreateContext();
        var navigation = context.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/weather?name=Nashville%2C%20TN&lat=36.1659&lng=-86.7844&tab=current");

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
                        FullSummary = "**Sunny** in Nashville.",
                        TemperatureF = 72,
                        WindSpeedMPH = 5,
                        WindDirection = "S",
                        WindDirectionDegrees = 180,
                        Conditions = "Clear",
                        Latitude = 36.1659,
                        Longitude = -86.7844,
                    }),
                });
            }

            if (path.Contains("/Forecast", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new UIWeatherForecastResponse
                    {
                        Latitude = 36.1659,
                        Longitude = -86.7844,
                        Timezone = "America/Chicago",
                        Daily = new UIWeatherDailySeries
                        {
                            Time = ["2026-08-19", "2026-08-20"],
                            WeatherCode = [1, 1],
                            TemperatureHighF = [88.4, 90.0],
                            TemperatureLowF = [70.1, 71.0],
                            PrecipitationInch = [0.3, 0.0],
                            WindSpeedMPH = [12.3, 10.0],
                            WindDirectionDegrees = [224, 90],
                        },
                        Hourly = new UIWeatherHourlySeries
                        {
                            Time = ["2026-08-19T14:00"],
                            TemperatureF = [86.5],
                            PrecipitationInch = [0.0],
                            WeatherCode = [1],
                            WindSpeedMPH = [8.2],
                            WindDirectionDegrees = [180],
                        },
                        Minutely15 = new UIWeatherHourlySeries
                        {
                            Time = ["2026-08-19T14:15"],
                            TemperatureF = [86.7],
                            PrecipitationInch = [0.0],
                            WeatherCode = [1],
                            WindSpeedMPH = [8.5],
                            WindDirectionDegrees = [190],
                        },
                    }),
                });
            }

            if (path.Contains("/History", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new UIWeatherHistoryResponse
                    {
                        Latitude = 36.1659,
                        Longitude = -86.7844,
                        Timezone = "America/Chicago",
                        Daily = new UIWeatherDailySeries
                        {
                            Time = ["2026-08-19", "2026-08-20"],
                            WeatherCode = [1, 1],
                            TemperatureHighF = [88.4, 90.0],
                            TemperatureLowF = [70.1, 71.0],
                            PrecipitationInch = [0.3, 0.0],
                            WindSpeedMPH = [12.3, 10.0],
                            WindDirectionDegrees = [224, 90],
                        },
                        Hourly = new UIWeatherHourlySeries
                        {
                            Time = ["2026-08-19T14:00", "2026-08-19T16:00"],
                            TemperatureF = [86.5, 84.0],
                            PrecipitationInch = [0.0, 0.0],
                            WeatherCode = [1, 1],
                            WindSpeedMPH = [8.2, 7.0],
                            WindDirectionDegrees = [180, 170],
                        },
                    }),
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
