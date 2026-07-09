namespace WeatherBlazor.Data;

public class WeatherForecast
{
    public DateOnly Date { get; set; }

    public double TemperatureK { get; set; }

    public string? Summary { get; set; }
}
