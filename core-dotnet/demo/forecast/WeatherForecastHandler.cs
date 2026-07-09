using MediatR;

namespace Core.demo.forecast;

/// <summary>
/// Generates a sample multi-day forecast using the same summary vocabulary and
/// temperature range as the Weather API's /weatherforecast endpoint.
/// </summary>
public class WeatherForecastHandler : IRequestHandler<WeatherForecastEvent, WeatherForecast[]>
{
    private static readonly string[] Summaries =
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    public Task<WeatherForecast[]> Handle(WeatherForecastEvent request, CancellationToken cancellationToken)
    {
        var startDate = request.StartDate ?? DateOnly.FromDateTime(DateTime.Now);

        var forecast = Enumerable.Range(1, request.Days).Select(index => new WeatherForecast
        {
            Date = startDate.AddDays(index),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)],
        }).ToArray();

        return Task.FromResult(forecast);
    }
}
