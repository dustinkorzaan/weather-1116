using Core.Weather;

namespace Core.Tests.Weather;

public class WeatherUnitConversionTests
{
    [Theory]
    [InlineData(0, "N")]
    [InlineData(44, "NE")]
    [InlineData(90, "E")]
    [InlineData(180, "S")]
    [InlineData(270, "W")]
    public void DegreesToCompass_MapsSourceDegrees(int degrees, string expected)
    {
        Assert.Equal(expected, WeatherUnitConversion.DegreesToCompass(degrees));
    }
}
