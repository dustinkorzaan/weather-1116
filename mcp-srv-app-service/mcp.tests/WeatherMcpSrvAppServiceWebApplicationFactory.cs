using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace WeatherMcpSrvAppService.Tests;

public class WeatherMcpSrvAppServiceWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly Dictionary<string, string?> _settings = new();

    public WeatherMcpSrvAppServiceWebApplicationFactory WithSetting(string key, string? value)
    {
        _settings[key] = value;
        return this;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(_settings);
        });
    }
}
