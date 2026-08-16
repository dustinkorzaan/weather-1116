using Core.Weather.Events;
using Core.Weather.Handlers;

namespace Core.Tests.Weather;

public class GetPublicWeatherHistoryHandlerTests
{
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
        Assert.Contains("temperature_unit=fahrenheit", url);
        Assert.Contains("wind_speed_unit=mph", url);
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
