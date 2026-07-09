namespace Core.demo.forecast;

/// <summary>
/// Shared forecast contract used to keep the React, MVC, and Blazor front ends in
/// parity with the data returned by the Weather API's /weatherforecast endpoint.
/// </summary>
public class WeatherForecast
{
    public required DateOnly Date { get; set; }

    public required double TemperatureK { get; set; }

    public string? Summary { get; set; }
}
