namespace Core.Weather;

/// <summary>Open-Meteo query parameters that pin forecast/history series to metric units.</summary>
public static class OpenMeteoUnits
{
    public const string CelsiusKmhMm =
        "temperature_unit=celsius&wind_speed_unit=kmh&precipitation_unit=mm";

    public const string CelsiusKmh =
        "temperature_unit=celsius&wind_speed_unit=kmh";
}
