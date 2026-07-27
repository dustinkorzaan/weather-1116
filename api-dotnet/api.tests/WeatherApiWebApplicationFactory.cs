using Core.About;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace WeatherAPI.Tests;

public class WeatherApiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IAboutClient>();
            services.AddSingleton<IAboutClient, StubAboutClient>();
        });
    }

    private sealed class StubAboutClient : IAboutClient
    {
        public Task<AboutNode> GetAsync(
            string? url,
            string expectedName,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new AboutNode { Name = expectedName });
    }
}
