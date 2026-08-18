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
        Assert.Equal("Wed, Aug 19", WeatherGridFormat.FormatCalendarDate("2026-08-19T14:00"));
    }

    [Fact]
    public void FormatClockTime_OmitsMinutesWhenZero()
    {
        Assert.Equal("2 PM", WeatherGridFormat.FormatClockTime("2026-08-19T14:00"));
    }

    [Fact]
    public void FormatClockTime_ShowsMinutesWhenNonZero()
    {
        Assert.Equal("2:15 PM", WeatherGridFormat.FormatClockTime("2026-08-19T14:15"));
    }

    [Fact]
    public void FormatPrecipitationIn_RoundsToNearestSixteenth()
    {
        Assert.Equal("1\"", WeatherGridFormat.FormatPrecipitationIn(1));
        Assert.Equal("0\"", WeatherGridFormat.FormatPrecipitationIn(0));
        Assert.Equal("1 1/2\"", WeatherGridFormat.FormatPrecipitationIn(1.5));
        Assert.Equal("2 1/4\"", WeatherGridFormat.FormatPrecipitationIn(2.25));
        Assert.Equal("3 5/16\"", WeatherGridFormat.FormatPrecipitationIn(3.3125));
        Assert.Equal("1/16\"", WeatherGridFormat.FormatPrecipitationIn(0.0625));
        Assert.Equal("5/16\"", WeatherGridFormat.FormatPrecipitationIn(0.3));
        Assert.Equal("2\"", WeatherGridFormat.FormatPrecipitationIn(1.9997));
    }

    [Fact]
    public void FormatPrecipitationIn_TreatsNegativeAndNonFiniteValuesAsZeroOrEmpty()
    {
        Assert.Equal("0\"", WeatherGridFormat.FormatPrecipitationIn(-0.5));
        Assert.Equal("0\"", WeatherGridFormat.FormatPrecipitationIn(-0.01));
        Assert.Equal(string.Empty, WeatherGridFormat.FormatPrecipitationIn(double.NaN));
        Assert.Equal(string.Empty, WeatherGridFormat.FormatPrecipitationIn(double.PositiveInfinity));
        Assert.Equal(string.Empty, WeatherGridFormat.FormatPrecipitationIn(double.NegativeInfinity));
    }

    [Fact]
    public void FormatTemperatureF_RoundsToOneDecimal()
    {
        Assert.Equal("75.2 °F", WeatherGridFormat.FormatTemperatureF(75.2));
        Assert.Equal("32 °F", WeatherGridFormat.FormatTemperatureF(32));
    }

    [Fact]
    public void FormatWindSpeedMph_RoundsToOneDecimal()
    {
        Assert.Equal("6.2 mph", WeatherGridFormat.FormatWindSpeedMph(6.2));
    }

    [Fact]
    public void FormatWindDirection_CombinesCompassAndDegrees()
    {
        Assert.Equal("SW (224°)", WeatherGridFormat.FormatWindDirection(224));
    }

    [Fact]
    public void WindArrowRotationDeg_MatchesWindToDegrees()
    {
        Assert.Equal(0, WeatherGridFormat.WindArrowRotationDeg(0));
        Assert.Equal(224, WeatherGridFormat.WindArrowRotationDeg(224));
        Assert.Null(WeatherGridFormat.WindArrowRotationDeg(double.NaN));
    }
}
