using Core.Weather;

namespace Core.Tests.Weather;

public class WeatherUnitConversionTests
{
    [Theory]
    [InlineData(0, 180)]
    [InlineData(90, 270)]
    [InlineData(180, 0)]
    [InlineData(224, 44)]
    [InlineData(270, 90)]
    [InlineData(360, 180)]
    public void MeteorologicalFromToWindTowards_Adds180AndWraps(int fromDegrees, int expectedTowardsDegrees)
    {
        Assert.Equal(expectedTowardsDegrees, WeatherUnitConversion.MeteorologicalFromToWindTowards(fromDegrees));
    }

    [Theory]
    [InlineData(0, "N")]
    [InlineData(44, "NE")]
    [InlineData(90, "E")]
    [InlineData(180, "S")]
    [InlineData(270, "W")]
    public void DegreesToCompass_MapsTowardsDegrees(int degrees, string expected)
    {
        Assert.Equal(expected, WeatherUnitConversion.DegreesToCompass(degrees));
    }
}
