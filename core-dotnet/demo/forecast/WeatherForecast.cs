namespace Core.demo.forecast;

/// <summary>
/// Shared forecast contract used to keep the React, MVC, and Blazor front ends in
/// parity with the data returned by the Weather API's /weatherforecast endpoint.
/// </summary>
public class WeatherForecast
{
    public required DateOnly Date { get; set; }

    public required int TemperatureC { get; set; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    public string? Summary { get; set; }
}
