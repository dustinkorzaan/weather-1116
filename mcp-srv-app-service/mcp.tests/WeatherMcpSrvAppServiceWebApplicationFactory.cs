using Core.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace WeatherMcpSrvAppService.Tests;

public class WeatherMcpSrvAppServiceWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly Dictionary<string, string?> _settings = new();
    private string? _inMemoryDatabaseName;

    public WeatherMcpSrvAppServiceWebApplicationFactory WithSetting(string key, string? value)
    {
        _settings[key] = value;
        return this;
    }

    /// <summary>
    /// Swaps the SQL Server DbContext for an EF Core in-memory database so the user/pin MCP tools
    /// can run end to end without a real SQL Server.
    /// </summary>
    public WeatherMcpSrvAppServiceWebApplicationFactory WithInMemoryDatabase()
    {
        _inMemoryDatabaseName = $"McpSrvAppServiceTests-{Guid.NewGuid()}";
        return this;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(_settings);
        });

        if (_inMemoryDatabaseName is string databaseName)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<WX1116DbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<WX1116DbContext>>();
                services.AddDbContext<WX1116DbContext>(options => options.UseInMemoryDatabase(databaseName));
            });
        }
    }
}
