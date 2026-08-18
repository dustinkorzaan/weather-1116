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

    [Theory]
    [InlineData(0, 0)]
    [InlineData(360, 0)]
    [InlineData(-90, 270)]
    [InlineData(450, 90)]
    [InlineData(720, 0)]
    public void NormalizeSourceDegrees_MapsToZeroThroughThreeSixty(int degrees, int expected)
    {
        Assert.Equal(expected, WeatherUnitConversion.NormalizeSourceDegrees(degrees));
    }
}
