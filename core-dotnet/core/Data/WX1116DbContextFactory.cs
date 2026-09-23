using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Core.Data;

/// <summary>
/// Design-time only, for `dotnet ef migrations add`/`dotnet ef database update` run directly
/// against Core (which has no Program.cs of its own to act as an EF "startup project"). Not
/// used at runtime -- API and MVC each register <see cref="WX1116DbContext"/> themselves
/// via DI, pointed at DB_CONNECTION_STRING through ManagedIdentitySqlConnectionStringFactory.
/// </summary>
public class WX1116DbContextFactory : IDesignTimeDbContextFactory<WX1116DbContext>
{
    public WX1116DbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
            ?? "Server=(localdb)\\mssqllocaldb;Database=WX1116DesignTime;Trusted_Connection=True;";

        var optionsBuilder = new DbContextOptionsBuilder<WX1116DbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new WX1116DbContext(optionsBuilder.Options);
    }
}
