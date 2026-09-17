using System.Net;

namespace WeatherMcpSrvAppService.Tests;

public class WakeControllerTests : IClassFixture<WeatherMcpSrvAppServiceWebApplicationFactory>
{
    private readonly WeatherMcpSrvAppServiceWebApplicationFactory _factory;

    public WakeControllerTests(WeatherMcpSrvAppServiceWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_ReturnsOk()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/Wake");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
