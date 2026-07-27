using System.Net;

namespace WeatherAPI.Tests;

public class RootEndpointTests(WeatherApiWebApplicationFactory factory) : IClassFixture<WeatherApiWebApplicationFactory>
{
    [Fact]
    public async Task GetRoot_RedirectsToAbout()
    {
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/About", response.Headers.Location?.OriginalString);
    }
}
