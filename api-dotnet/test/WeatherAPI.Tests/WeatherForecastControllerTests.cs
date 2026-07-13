using System.Net;
using System.Net.Http.Json;
using Core.demo.forecast;

namespace WeatherAPI.Tests;

public class WeatherForecastControllerTests(WeatherApiWebApplicationFactory factory) : IClassFixture<WeatherApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Get_ReturnsFiveForecasts()
    {
        var response = await _client.GetAsync("/WeatherForecast");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var forecasts = await response.Content.ReadFromJsonAsync<WeatherForecast[]>();
        Assert.NotNull(forecasts);
        Assert.Equal(5, forecasts.Length);

        foreach (var forecast in forecasts)
        {
            Assert.InRange(forecast.TemperatureK, 250, 325);
            Assert.False(string.IsNullOrWhiteSpace(forecast.Summary));
        }
    }

    [Fact]
    public async Task Get_WithStartDate_UsesProvidedStartDate()
    {
        var startDate = new DateTime(2026, 1, 15);
        var response = await _client.GetAsync($"/WeatherForecast?startDate={startDate:yyyy-MM-dd}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var forecasts = await response.Content.ReadFromJsonAsync<WeatherForecast[]>();
        Assert.NotNull(forecasts);
        Assert.Equal(DateOnly.FromDateTime(startDate.AddDays(1)), forecasts[0].Date);
    }
}
