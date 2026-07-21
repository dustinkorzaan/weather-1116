using Core.about;
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
            services.RemoveAll<IMcpAboutClient>();
            services.AddSingleton<IMcpAboutClient, StubMcpAboutClient>();
        });
    }

    private sealed class StubMcpAboutClient : IMcpAboutClient
    {
        public Task<AboutNode> GetAsync(
            string? url,
            string expectedName,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new AboutNode { Name = expectedName });
    }
}
