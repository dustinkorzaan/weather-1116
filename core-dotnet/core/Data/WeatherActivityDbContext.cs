using Core.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace Core.Data;

public class WeatherActivityDbContext(DbContextOptions<WeatherActivityDbContext> options) : DbContext(options)
{
    public DbSet<WeatherActivity> WeatherActivity => Set<WeatherActivity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WeatherActivityDbContext).Assembly);
    }
}
