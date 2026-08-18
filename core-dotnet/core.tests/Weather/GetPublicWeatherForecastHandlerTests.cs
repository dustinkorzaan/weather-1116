using Core.Weather.Events;
using Core.Weather.Handlers;

namespace Core.Tests.Weather;

public class GetPublicWeatherForecastHandlerTests
{
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
