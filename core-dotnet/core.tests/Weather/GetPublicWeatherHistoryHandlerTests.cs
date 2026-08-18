using System.Net;
using System.Text;
using Core.Caching;
using Core.Http;
using Core.Weather.Events;
using Core.Weather.Handlers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Core.Tests.Weather;

public class GetPublicWeatherHistoryHandlerTests
{
    [Fact]
    public async Task Handle_OpenMeteoReturnsNullSeriesArrays_NormalizesToEmptyLists()
    {
        const string json = """
        {
          "latitude": 36.16,
          "longitude": -86.78,
          "timezone": "America/Chicago",
          "hourly": {
            "time": null,
            "temperature_2m": null,
            "precipitation": null,
            "weather_code": null,
            "wind_speed_10m": null,
            "wind_direction_10m": null
          }
        }
        """;
        var handler = CreateHandler(json);

        var response = await handler.Handle(
            new GetPublicWeatherHistoryEvent { Latitude = 36.16, Longitude = -86.78, Resolution = PublicWeatherHistoryResolution.Hourly },
            CancellationToken.None);

        Assert.NotNull(response.Hourly);
        Assert.Empty(response.Hourly!.Time);
        Assert.Empty(response.Hourly.Temperature2m);
        Assert.Empty(response.Hourly.Precipitation);
        Assert.Empty(response.Hourly.WeatherCode);
        Assert.Empty(response.Hourly.WindSpeed10m);
        Assert.Empty(response.Hourly.WindDirection10m);
    }

    [Fact]
    public async Task Handle_OpenMeteoReturnsNegativePrecipitation_ClampsToZero()
    {
        const string json = """
        {
          "latitude": 36.16,
          "longitude": -86.78,
          "timezone": "America/Chicago",
          "hourly": {
            "time": ["2026-08-19T14:00"],
            "temperature_2m": [75.2],
            "precipitation": [-0.1, 0.3],
            "weather_code": [0],
            "wind_speed_10m": [6.2],
            "wind_direction_10m": [224]
          }
        }
        """;
        var handler = CreateHandler(json);

        var response = await handler.Handle(
            new GetPublicWeatherHistoryEvent { Latitude = 36.16, Longitude = -86.78, Resolution = PublicWeatherHistoryResolution.Hourly },
            CancellationToken.None);

        Assert.Equal([0, 0.3], response.Hourly!.Precipitation);
    }

    private static GetPublicWeatherHistoryHandler CreateHandler(string json) =>
        new(
            new CacheHelper(new MemoryCache(new MemoryCacheOptions())),
            new TransientRetryHelper(NullLogger<TransientRetryHelper>.Instance),
            new FakeHttpClientFactory(new StaticResponseHandler(json)));

    private sealed class FakeHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler);
    }

    private sealed class StaticResponseHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            });
    }

    [Fact]
    public void BuildHistoryUrl_Daily_UsesPreviousSevenDays()
    {
        var url = GetPublicWeatherHistoryHandler.BuildHistoryUrl(
            36.1627,
            -86.7816,
            PublicWeatherHistoryResolution.Daily);

        Assert.StartsWith("https://api.open-meteo.com/v1/forecast?", url);
        Assert.Contains("latitude=36.1627", url);
        Assert.Contains("longitude=-86.7816", url);
        Assert.Contains("daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_sum,wind_speed_10m_max,wind_direction_10m_dominant", url);
        Assert.Contains("past_days=7", url);
        Assert.Contains("forecast_days=0", url);
        Assert.Contains("timezone=auto", url);
        Assert.DoesNotContain("hourly=", url);
        Assert.DoesNotContain("latitude=36,1627", url);
        Assert.DoesNotContain("longitude=-86,7816", url);
    }

    [Fact]
    public void BuildHistoryUrl_Hourly_UsesPreviousFortyEightHours()
    {
        var url = GetPublicWeatherHistoryHandler.BuildHistoryUrl(
            36.1627,
            -86.7816,
            PublicWeatherHistoryResolution.Hourly);

        Assert.Contains("hourly=temperature_2m,precipitation,weather_code,wind_speed_10m,wind_direction_10m", url);
        Assert.Contains("past_hours=48", url);
        Assert.Contains("forecast_hours=0", url);
        Assert.Contains("timezone=auto", url);
        Assert.DoesNotContain("daily=", url);
    }
}
