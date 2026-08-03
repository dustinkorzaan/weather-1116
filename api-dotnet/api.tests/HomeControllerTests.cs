using System.Net;
using System.Net.Http.Json;
using Core.HelloWorld.Models;

namespace WeatherAPI.Tests;

public class HomeControllerTests(WeatherApiWebApplicationFactory factory) : IClassFixture<WeatherApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Hello_ReturnsOkWithExpectedMessage()
    {
        var response = await _client.GetAsync("/Home/Hello");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<HelloWorldResponse>();
        Assert.NotNull(body);
        Assert.Equal("from WeatherAPI", body.RequestMessage);
        Assert.Contains("Hello, from WeatherAPI", body.RequestResponse);
        Assert.True(body.TimestampUtc <= DateTime.UtcNow);
    }
}
