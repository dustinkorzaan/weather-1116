using Core.Weather.Events;
using Core.Weather.Handlers;

namespace Core.Tests.Weather;

public class GetPublicWeatherCurrentHandlerTests
{
    [Fact]
    public void BuildCurrentWeatherUrl_UsesInvariantCoordinates()
    {
        var url = GetPublicWeatherCurrentHandler.BuildCurrentWeatherUrl(36.1627, -86.7816);

        Assert.StartsWith("https://api.open-meteo.com/v1/forecast?", url);
        Assert.Contains("latitude=36.1627", url);
        Assert.Contains("longitude=-86.7816", url);
        Assert.Contains("current_weather=true", url);
        Assert.Contains("temperature_unit=fahrenheit", url);
        Assert.Contains("wind_speed_unit=mph", url);
        Assert.Contains("precipitation_unit=inch", url);
        Assert.DoesNotContain("timezone=", url);
        Assert.DoesNotContain(',', url);
    }
}
