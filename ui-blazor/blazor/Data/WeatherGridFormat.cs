namespace WeatherBlazor.Data;

using System.Globalization;

/// <summary>Formatting helpers for the weather modal's forecast/history grid tabs.</summary>
public static class WeatherGridFormat
{
    private static readonly string[] CompassPoints =
    [
        "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE",
        "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW",
    ];

    /// <summary>Converts meteorological from-degrees (0° = from the north) to a 16-point compass abbreviation.</summary>
    public static string DegreesToCompass(double degrees)
    {
        var normalized = ((degrees % 360) + 360) % 360;
        var index = (int)Math.Round(normalized / 22.5) % 16;
        return CompassPoints[index];
    }

    /// <summary>Formats an Open-Meteo daily date ("2026-08-19") as "Wed, Aug 19".</summary>
    public static string FormatCalendarDate(string isoDate)
    {
        if (!DateTime.TryParse(isoDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return isoDate ?? string.Empty;
        }

        return date.ToString("ddd, MMM d", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Formats an Open-Meteo hourly/15-minute timestamp ("2026-08-19T14:00") as "2 PM"
    /// (minutes shown only when non-zero, e.g. "2:15 PM").
    /// </summary>
    public static string FormatClockTime(string isoDateTime)
    {
        if (!DateTime.TryParse(isoDateTime, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
        {
            return isoDateTime ?? string.Empty;
        }

        return dateTime.ToString(dateTime.Minute == 0 ? "h tt" : "h:mm tt", CultureInfo.InvariantCulture);
    }

    /// <summary>Formats an already-converted inches value (the API returns US customary units) rounded to the nearest 1/16", e.g. "1 1/2"". Negative values (an upstream data artifact) are treated as zero.</summary>
    public static string FormatPrecipitationIn(double inches)
    {
        if (double.IsNaN(inches) || double.IsInfinity(inches))
        {
            return string.Empty;
        }

        var sixteenths = (long)Math.Round(Math.Max(0, inches) * 16, MidpointRounding.AwayFromZero);
        var whole = sixteenths / 16;
        var remainder = sixteenths % 16;

        if (remainder == 0)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{whole}\"");
        }

        var (numerator, denominator) = ReduceSixteenths(remainder);
        return whole == 0
            ? string.Create(CultureInfo.InvariantCulture, $"{numerator}/{denominator}\"")
            : string.Create(CultureInfo.InvariantCulture, $"{whole} {numerator}/{denominator}\"");
    }

    /// <summary>Reduces a sixteenths-of-an-inch numerator to lowest terms (denominator is always a power of two).</summary>
    private static (long Numerator, long Denominator) ReduceSixteenths(long numerator)
    {
        long denominator = 16;
        while (numerator != 0 && numerator % 2 == 0 && denominator > 1)
        {
            numerator /= 2;
            denominator /= 2;
        }
        return (numerator, denominator);
    }

    /// <summary>Formats an already-converted °F value (the API returns US customary units).</summary>
    public static string FormatTemperatureF(double fahrenheit) =>
        string.Create(CultureInfo.InvariantCulture, $"{Math.Round(fahrenheit, 1)} °F");

    /// <summary>Formats an already-converted mph value (the API returns US customary units).</summary>
    public static string FormatWindSpeedMph(double mph) =>
        string.Create(CultureInfo.InvariantCulture, $"{Math.Round(mph, 1)} mph");

    /// <summary>Formats from-degrees as compass plus degrees, e.g. "SW (224°)".</summary>
    public static string FormatWindDirection(double degrees) =>
        string.Create(CultureInfo.InvariantCulture, $"{DegreesToCompass(degrees)} ({Math.Round(degrees):0}°)");

    /// <summary>
    /// Rotation for the ⮙ wind arrow; adds 180° to from-degrees so the icon points where wind blows.
    /// </summary>
    public static int? WindArrowRotationDeg(double fromDegrees)
    {
        if (double.IsNaN(fromDegrees) || double.IsInfinity(fromDegrees))
        {
            return null;
        }

        var normalized = ((fromDegrees % 360) + 360) % 360;
        return (int)Math.Round((normalized + 180) % 360);
    }
}
