using MediatR;

namespace Core.demo.forecast;

/// <summary>
/// Requests a generated multi-day weather forecast, mirroring the shape returned by
/// the Weather API's /weatherforecast endpoint so apps without direct API access
/// (e.g. WeatherMVC) can present the same forecast data.
/// </summary>
public class WeatherForecastEvent : IRequest<WeatherForecast[]>
{
    /// <summary>Number of days to forecast, starting the day after <see cref="StartDate"/>.</summary>
    public int Days { get; set; } = 5;

    /// <summary>The date the forecast is generated from. Defaults to today when not set.</summary>
    public DateOnly? StartDate { get; set; }
}
