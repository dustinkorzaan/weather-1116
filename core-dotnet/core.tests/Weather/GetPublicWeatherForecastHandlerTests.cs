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

public class GetPublicWeatherForecastHandlerTests
{
    [Fact]
    public async Task Handle_OpenMeteoReturnsNullSeriesArrays_NormalizesToEmptyLists()
    {
        const string json = """
        {
          "latitude": 36.16,
          "longitude": -86.78,
          "timezone": "America/Chicago",
          "daily": {
            "time": null,
            "weather_code": null,
            "temperature_2m_max": null,
            "temperature_2m_min": null,
            "precipitation_sum": null,
            "wind_speed_10m_max": null,
            "wind_direction_10m_dominant": null
          }
        }
        """;
        var handler = CreateHandler(json);

        var response = await handler.Handle(
            new GetPublicWeatherForecastEvent { Latitude = 36.16, Longitude = -86.78, Resolution = PublicWeatherForecastResolution.Daily },
            CancellationToken.None);

        Assert.NotNull(response.Daily);
        Assert.Empty(response.Daily!.Time);
        Assert.Empty(response.Daily.WeatherCode);
        Assert.Empty(response.Daily.Temperature2mMax);
        Assert.Empty(response.Daily.Temperature2mMin);
        Assert.Empty(response.Daily.PrecipitationSum);
        Assert.Empty(response.Daily.WindSpeed10mMax);
        Assert.Empty(response.Daily.WindDirection10mDominant);
    }

    [Fact]
    public async Task Handle_OpenMeteoReturnsNegativePrecipitation_ClampsToZero()
    {
        const string json = """
        {
          "latitude": 36.16,
          "longitude": -86.78,
          "timezone": "America/Chicago",
          "daily": {
            "time": ["2026-08-19"],
            "weather_code": [0],
            "temperature_2m_max": [88.4],
            "temperature_2m_min": [66.0],
            "precipitation_sum": [-0.1, 0.3],
            "wind_speed_10m_max": [6.2],
            "wind_direction_10m_dominant": [224]
          }
        }
        """;
        var handler = CreateHandler(json);

        var response = await handler.Handle(
            new GetPublicWeatherForecastEvent { Latitude = 36.16, Longitude = -86.78, Resolution = PublicWeatherForecastResolution.Daily },
            CancellationToken.None);

        Assert.Equal([0, 0.3], response.Daily!.PrecipitationSum);
    }

    private static GetPublicWeatherForecastHandler CreateHandler(string json) =>
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
    public void BuildForecastUrl_Daily_UsesSevenDaysAndAutoTimezone()
    {
        var url = GetPublicWeatherForecastHandler.BuildForecastUrl(
            36.1627,
            -86.7816,
            PublicWeatherForecastResolution.Daily);

        Assert.StartsWith("https://api.open-meteo.com/v1/forecast?", url);
        Assert.Contains("latitude=36.1627", url);
        Assert.Contains("longitude=-86.7816", url);
        Assert.Contains("daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_sum,wind_speed_10m_max,wind_direction_10m_dominant", url);
        Assert.Contains("forecast_days=7", url);
        Assert.Contains("timezone=auto", url);
        Assert.DoesNotContain("hourly=", url);
        Assert.DoesNotContain("minutely_15=", url);
        Assert.DoesNotContain("latitude=36,1627", url);
        Assert.DoesNotContain("longitude=-86,7816", url);
    }

    [Fact]
    public void BuildForecastUrl_Hourly_UsesFortyEightHours()
    {
        var url = GetPublicWeatherForecastHandler.BuildForecastUrl(
            36.1627,
            -86.7816,
            PublicWeatherForecastResolution.Hourly);

        Assert.Contains("hourly=temperature_2m,precipitation,weather_code,wind_speed_10m,wind_direction_10m", url);
        Assert.Contains("forecast_hours=48", url);
        Assert.Contains("timezone=auto", url);
        Assert.DoesNotContain("daily=", url);
        Assert.DoesNotContain("minutely_15=", url);
    }

    [Fact]
    public void BuildForecastUrl_FifteenMinutes_UsesFortyEightHours()
    {
        var url = GetPublicWeatherForecastHandler.BuildForecastUrl(
            36.1627,
            -86.7816,
            PublicWeatherForecastResolution.FifteenMinutes);

        Assert.Contains("minutely_15=temperature_2m,precipitation,weather_code,wind_speed_10m,wind_direction_10m", url);
        Assert.Contains("forecast_minutely_15=192", url);
        Assert.Contains("timezone=auto", url);
        Assert.DoesNotContain("daily=", url);
        Assert.DoesNotContain("hourly=", url);
    }
}
