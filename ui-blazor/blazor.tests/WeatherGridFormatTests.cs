using WeatherBlazor.Data;

namespace WeatherBlazor.Tests;

public sealed class WeatherGridFormatTests
{
    [Theory]
    [InlineData(0, "N")]
    [InlineData(90, "E")]
    [InlineData(180, "S")]
    [InlineData(224, "SW")]
    [InlineData(360, "N")]
    public void DegreesToCompass_MapsToSixteenPointCompass(double degrees, string expected)
    {
        Assert.Equal(expected, WeatherGridFormat.DegreesToCompass(degrees));
    }

    [Fact]
    public void FormatCalendarDate_FormatsAsWeekdayMonthDay()
    {
        Assert.Equal("Wed, Aug 19", WeatherGridFormat.FormatCalendarDate("2026-08-19"));
    }

    [Fact]
    public void FormatClockTime_OmitsMinutesWhenZero()
    {
        Assert.Equal("Wed, Aug 19, 2 PM", WeatherGridFormat.FormatClockTime("2026-08-19T14:00"));
    }

    [Fact]
    public void FormatClockTime_ShowsMinutesWhenNonZero()
    {
        Assert.Equal("Wed, Aug 19, 2:15 PM", WeatherGridFormat.FormatClockTime("2026-08-19T14:15"));
    }

    [Fact]
    public void FormatPrecipitationMm_RoundsAwayFloatingPointNoise()
    {
        Assert.Equal("0.3 mm", WeatherGridFormat.FormatPrecipitationMm(0.30000000000000004));
    }

    [Fact]
    public void FormatTemperatureC_RoundsToOneDecimal()
    {
        Assert.Equal("88.4 °C", WeatherGridFormat.FormatTemperatureC(88.44));
    }

    [Fact]
    public void FormatWindSpeedKmh_RoundsToOneDecimal()
    {
        Assert.Equal("12.3 km/h", WeatherGridFormat.FormatWindSpeedKmh(12.34));
    }

    [Fact]
    public void FormatWindDirection_CombinesCompassAndDegrees()
    {
        Assert.Equal("SW (224°)", WeatherGridFormat.FormatWindDirection(224));
    }
}
