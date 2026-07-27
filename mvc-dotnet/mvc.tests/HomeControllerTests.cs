using System.Net;

namespace WeatherMVC.Tests;

public class HomeControllerTests(WeatherMvcWebApplicationFactory factory) : IClassFixture<WeatherMvcWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Index_ReturnsOkWithHelloMessage()
    {
        var response = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Hello, from WeatherMVC", html);
    }
}
