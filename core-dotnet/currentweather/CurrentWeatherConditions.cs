namespace Core.currentweather;

/// <summary>
/// Current observed weather conditions for a given location, sourced from Open Meteo.
/// </summary>
public class CurrentWeatherConditions
{
    public required string Location { get; set; }
    public required double Latitude { get; set; }
    public required double Longitude { get; set; }

    /// <summary>Temperature in degrees Celsius.</summary>
    public required double TemperatureC { get; set; }

    /// <summary>Wind speed in km/h.</summary>
    public required double WindSpeedKph { get; set; }

    /// <summary>Wind direction in degrees (0–360).</summary>
    public required int WindDirectionDeg { get; set; }

    /// <summary>Whether it is currently daytime at the location.</summary>
    public required bool IsDay { get; set; }

    /// <summary>WMO weather interpretation code.</summary>
    public required int WeatherCode { get; set; }

    /// <summary>ISO 8601 timestamp of the observation.</summary>
    public required string ObservedAt { get; set; }
}
