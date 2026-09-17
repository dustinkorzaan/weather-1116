using System.Net;

namespace WeatherAPI.Tests;

public class WakeControllerTests(WeatherApiWebApplicationFactory factory) : IClassFixture<WeatherApiWebApplicationFactory>
{
    [Fact]
    public async Task Get_ReturnsOk()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/Wake");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
