namespace WeatherMVC.Models;

public class HomeIndexViewModel
{
    public string? HelloResponse { get; set; }

    public IReadOnlyList<WeatherForecastViewModel> Forecasts { get; set; } = [];
}

public class WeatherForecastViewModel
{
    public DateOnly Date { get; set; }

    public int TemperatureC { get; set; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    public string? Summary { get; set; }
}
