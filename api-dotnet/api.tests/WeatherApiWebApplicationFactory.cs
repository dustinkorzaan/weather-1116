using Core.About;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace WeatherAPI.Tests;

public class WeatherApiWebApplicationFactory : WebApplicationFactory<Program>
{
    public WeatherApiWebApplicationFactory()
    {
        // Set DB_CONNECTION_STRING environment variable before the app starts.
        // This must happen before Program.cs runs Env.TraversePath().Load()
        Environment.SetEnvironmentVariable("DB_CONNECTION_STRING",
            "Server=(localdb)\\mssqllocaldb;Database=WeatherApiTest;Integrated Security=true;",
            EnvironmentVariableTarget.Process);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IAboutClient>();
            services.AddSingleton<IAboutClient, StubAboutClient>();
        });
    }

    internal sealed class StubAboutClient : IAboutClient
    {
        public Task<AboutNode> GetAsync(
            string? url,
            string expectedName,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new AboutNode { Name = expectedName });
    }
}
