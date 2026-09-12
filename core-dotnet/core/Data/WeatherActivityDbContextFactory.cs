using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Core.Data;

/// <summary>
/// Design-time only, for `dotnet ef migrations add`/`dotnet ef database update` run directly
/// against Core (which has no Program.cs of its own to act as an EF "startup project"). Not
/// used at runtime -- API and MVC each register <see cref="WeatherActivityDbContext"/> themselves
/// via DI, pointed at DB_CONNECTION_STRING through ManagedIdentitySqlConnectionStringFactory.
/// </summary>
public class WeatherActivityDbContextFactory : IDesignTimeDbContextFactory<WeatherActivityDbContext>
{
    public WeatherActivityDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
            ?? "Server=(localdb)\\mssqllocaldb;Database=WeatherActivityDesignTime;Trusted_Connection=True;";

        var optionsBuilder = new DbContextOptionsBuilder<WeatherActivityDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new WeatherActivityDbContext(optionsBuilder.Options);
    }
}
