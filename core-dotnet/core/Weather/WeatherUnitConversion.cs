namespace Core.Weather;

/// <summary>Open-Meteo metric → US customary unit conversions shared by the UI-facing weather mappers.</summary>
public static class WeatherUnitConversion
{
    /// <summary>Open-Meteo °C → °F.</summary>
    public static double CelsiusToFahrenheit(double celsius) => Math.Round(celsius * 9d / 5d + 32d, 1);

    /// <summary>Open-Meteo km/h → mph.</summary>
    public static double KilometersPerHourToMph(double kilometersPerHour) => Math.Round(kilometersPerHour / 1.609344, 1);

    /// <summary>Open-Meteo mm → inches.</summary>
    public static double MillimetersToInches(double millimeters) => Math.Round(millimeters / 25.4, 2);

    /// <summary>
    /// Meteorological from-direction (0° = from the north) → destination heading
    /// (0° = toward the north). Adds 180° and wraps to 0–359.
    /// </summary>
    public static int MeteorologicalFromToWindTo(int fromDegrees)
    {
        var normalized = ((fromDegrees % 360) + 360) % 360;
        return (normalized + 180) % 360;
    }

    private static readonly string[] CompassPoints =
    [
        "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE",
        "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW",
    ];

    /// <summary>Converts wind-to degrees (0° = toward north) to a 16-point compass abbreviation.</summary>
    public static string DegreesToCompass(int degrees)
    {
        var normalized = ((degrees % 360) + 360) % 360;
        var index = (int)Math.Round(normalized / 22.5) % 16;
        return CompassPoints[index];
    }
}
