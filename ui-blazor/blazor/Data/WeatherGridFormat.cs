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

    /// <summary>Converts meteorological degrees to a 16-point compass abbreviation.</summary>
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

    /// <summary>Open-Meteo °C → °F.</summary>
    public static double CelsiusToFahrenheit(double celsius) => celsius * 9d / 5d + 32d;

    /// <summary>Open-Meteo km/h → mph.</summary>
    public static double KilometersPerHourToMph(double kilometersPerHour) =>
        kilometersPerHour / 1.609344;

    /// <summary>Open-Meteo mm → inches.</summary>
    public static double MillimetersToInches(double millimeters) => millimeters / 25.4;

    public static string FormatPrecipitationIn(double millimeters) =>
        string.Create(CultureInfo.InvariantCulture, $"{Math.Round(MillimetersToInches(millimeters), 2)}\"");

    public static string FormatTemperatureF(double celsius) =>
        string.Create(CultureInfo.InvariantCulture, $"{Math.Round(CelsiusToFahrenheit(celsius), 1)} °F");

    public static string FormatWindSpeedMph(double kilometersPerHour) =>
        string.Create(CultureInfo.InvariantCulture, $"{Math.Round(KilometersPerHourToMph(kilometersPerHour), 1)} mph");

    /// <summary>Formats meteorological degrees as compass plus degrees, e.g. "SW (224°)".</summary>
    public static string FormatWindDirection(double degrees) =>
        string.Create(CultureInfo.InvariantCulture, $"{DegreesToCompass(degrees)} ({Math.Round(degrees):0}°)");

    /// <summary>
    /// Rotation for the ➤ wind arrow so 0° (north / from the north) points up.
    /// </summary>
    public static int? WindArrowRotationDeg(double degrees)
    {
        if (double.IsNaN(degrees) || double.IsInfinity(degrees))
        {
            return null;
        }

        return (int)Math.Round(degrees) - 90;
    }
}
