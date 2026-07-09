using MediatR;

namespace Core.demo.forecast;

/// <summary>
/// Generates a sample multi-day forecast using the same summary vocabulary as
/// the Weather API and a direct Kelvin range from 250 to 325.
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

        var forecast = Enumerable.Range(1, request.Days).Select(index =>
        {
            var temperatureK = Random.Shared.Next(250, 326);

            return new WeatherForecast
            {
                Date = startDate.AddDays(index),
                TemperatureK = temperatureK,
                Summary = Summaries[Random.Shared.Next(Summaries.Length)],
            };
        }).ToArray();

        return Task.FromResult(forecast);
    }
}
