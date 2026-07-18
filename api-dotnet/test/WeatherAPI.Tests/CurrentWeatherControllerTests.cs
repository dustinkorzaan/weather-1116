using System.Net;
using System.Net.Http.Json;
using Core.currentweather;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace WeatherAPI.Tests;

public class CurrentWeatherControllerTests : IClassFixture<WeatherApiWebApplicationFactory>
{
    private readonly WeatherApiWebApplicationFactory _factory;

    public CurrentWeatherControllerTests(WeatherApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithStub(CurrentWeatherConditions stubResult)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ICurrentWeatherSource>();
                services.AddSingleton<ICurrentWeatherSource>(new StubCurrentWeatherSource(stubResult));
            });
        }).CreateClient();
    }

    [Fact]
    public async Task Get_WithLocation_ReturnsConditions()
    {
        var expected = new CurrentWeatherConditions
        {
            Location = "New York, NY",
            Latitude = 40.7128,
            Longitude = -74.006,
            TemperatureC = 22.5,
            WindSpeedKph = 14.0,
            WindDirectionDeg = 90,
            IsDay = true,
            WeatherCode = 1,
            ObservedAt = "2026-07-18T12:00",
        };

        var client = CreateClientWithStub(expected);
        var response = await client.GetAsync("/CurrentWeather?location=New+York%2C+NY");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<CurrentWeatherConditions>();
        Assert.NotNull(result);
        Assert.Equal("New York, NY", result.Location);
        Assert.Equal(22.5, result.TemperatureC);
        Assert.Equal(14.0, result.WindSpeedKph);
        Assert.True(result.IsDay);
    }

    [Fact]
    public async Task Get_WithoutLocation_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/CurrentWeather");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_WithEmptyLocation_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/CurrentWeather?location=");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private sealed class StubCurrentWeatherSource(CurrentWeatherConditions result) : ICurrentWeatherSource
    {
        public Task<CurrentWeatherConditions> GetCurrentWeatherAsync(string location, CancellationToken cancellationToken)
            => Task.FromResult(result);
    }
}
