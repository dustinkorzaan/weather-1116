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
    public void FormatPrecipitationIn_ConvertsMillimetersAndRounds()
    {
        Assert.Equal("1\"", WeatherGridFormat.FormatPrecipitationIn(25.4));
        Assert.Equal("0.3\"", WeatherGridFormat.FormatPrecipitationIn(7.62));
    }

    [Fact]
    public void FormatTemperatureF_ConvertsCelsiusAndRoundsToOneDecimal()
    {
        Assert.Equal("75.2 °F", WeatherGridFormat.FormatTemperatureF(24));
        Assert.Equal("32 °F", WeatherGridFormat.FormatTemperatureF(0));
    }

    [Fact]
    public void FormatWindSpeedMph_ConvertsKilometersPerHourAndRoundsToOneDecimal()
    {
        Assert.Equal("6.2 mph", WeatherGridFormat.FormatWindSpeedMph(10));
    }

    [Fact]
    public void FormatWindDirection_CombinesCompassAndDegrees()
    {
        Assert.Equal("SW (224°)", WeatherGridFormat.FormatWindDirection(224));
    }
}
