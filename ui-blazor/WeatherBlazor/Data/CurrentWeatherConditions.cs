namespace WeatherBlazor.Data;

/// <summary>
/// Current observed weather conditions returned by the WeatherAPI /CurrentWeather endpoint.
/// Mirrors Core.currentweather.CurrentWeatherConditions on the .NET API side.
/// </summary>
public class CurrentWeatherConditions
{
    public string Location { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    /// <summary>Temperature in degrees Celsius.</summary>
    public double TemperatureC { get; set; }

    /// <summary>Wind speed in km/h.</summary>
    public double WindSpeedKph { get; set; }

    /// <summary>Wind direction in degrees (0–360).</summary>
    public int WindDirectionDeg { get; set; }

    public bool IsDay { get; set; }
    public int WeatherCode { get; set; }

    /// <summary>ISO 8601 timestamp of the observation.</summary>
    public string ObservedAt { get; set; } = string.Empty;
}
