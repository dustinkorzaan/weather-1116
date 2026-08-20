using WeatherBlazor.Data;

namespace WeatherBlazor.Tests;

public sealed class WeatherGridFormatTests
{
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
        Assert.Equal("SW (224°)", WeatherGridFormat.FormatWindDirection("SW", 224));
        Assert.Equal("S (180°)", WeatherGridFormat.FormatWindDirection("S", 540));
        Assert.Equal("N (0°)", WeatherGridFormat.FormatWindDirection("N", double.NaN));
        Assert.Equal("(0°)", WeatherGridFormat.FormatWindDirection("  ", double.PositiveInfinity));
    }

    [Fact]
    public void NormalizeSourceDegrees_WrapsAndTreatsNonFiniteAsZero()
    {
        Assert.Equal(224, WeatherGridFormat.NormalizeSourceDegrees(224));
        Assert.Equal(180, WeatherGridFormat.NormalizeSourceDegrees(540));
        Assert.Equal(270, WeatherGridFormat.NormalizeSourceDegrees(-90));
        Assert.Equal(0, WeatherGridFormat.NormalizeSourceDegrees(360));
        Assert.Equal(0, WeatherGridFormat.NormalizeSourceDegrees(double.NaN));
        Assert.Equal(0, WeatherGridFormat.NormalizeSourceDegrees(double.PositiveInfinity));
        Assert.Equal(0, WeatherGridFormat.NormalizeSourceDegrees(double.NegativeInfinity));
    }

    [Fact]
    public void FormatRunLogTimestamp_FormatsAsUtcTimeOfDayWithMilliseconds()
    {
        var utc = new DateTime(2026, 8, 19, 14, 32, 7, 123, DateTimeKind.Utc);
        Assert.Equal("14:32:07.123", WeatherGridFormat.FormatRunLogTimestamp(utc));
    }

    [Fact]
    public void FormatRunLogTimestamp_ConvertsNonUtcKindToUtc()
    {
        var unspecified = new DateTime(2026, 8, 19, 14, 32, 7, 123, DateTimeKind.Unspecified);
        Assert.Equal(
            unspecified.ToUniversalTime().ToString("HH:mm:ss.fff"),
            WeatherGridFormat.FormatRunLogTimestamp(unspecified));
    }

    [Fact]
    public void FormatRunLogMs_FormatsWithThousandsSeparators()
    {
        Assert.Equal("0", WeatherGridFormat.FormatRunLogMs(0));
        Assert.Equal("1,234", WeatherGridFormat.FormatRunLogMs(1234));
    }

    [Fact]
    public void FormatRunLogTokenCount_FormatsWithThousandsSeparators()
    {
        Assert.Equal("0", WeatherGridFormat.FormatRunLogTokenCount(0));
        Assert.Equal("1,234", WeatherGridFormat.FormatRunLogTokenCount(1234));
        Assert.Equal(string.Empty, WeatherGridFormat.FormatRunLogTokenCount(null));
    }

    [Fact]
    public void FormatChatRuntime_UsesMsBelowOneSecondAndSecondsAfter()
    {
        Assert.Equal("0ms", WeatherGridFormat.FormatChatRuntime(0));
        Assert.Equal("842ms", WeatherGridFormat.FormatChatRuntime(842));
        Assert.Equal("1s", WeatherGridFormat.FormatChatRuntime(1000));
        Assert.Equal("1.24s", WeatherGridFormat.FormatChatRuntime(1240));
        Assert.Equal("10s", WeatherGridFormat.FormatChatRuntime(10000));
    }

    [Fact]
    public void FormatChatUsageChip_CombinesRuntimeAndTotalTokens()
    {
        Assert.Equal(
            "1.24s · 4,218 tok",
            WeatherGridFormat.FormatChatUsageChip(new ChatUsage
            {
                RuntimeMs = 1240,
                TotalTokenCount = 4218,
            }));
        Assert.Equal("842ms", WeatherGridFormat.FormatChatUsageChip(new ChatUsage { RuntimeMs = 842 }));
        Assert.Equal(string.Empty, WeatherGridFormat.FormatChatUsageChip(null));
    }

    [Fact]
    public void FormatChatUsageDetails_OmitsMissingTokenFields()
    {
        Assert.Equal(
            "Runtime: 1,240 ms\nInput: 3,100\nTotal: 4,218",
            WeatherGridFormat.FormatChatUsageDetails(new ChatUsage
            {
                RuntimeMs = 1240,
                InputTokenCount = 3100,
                TotalTokenCount = 4218,
            }));
        Assert.Equal(string.Empty, WeatherGridFormat.FormatChatUsageDetails(null));
    }
}
