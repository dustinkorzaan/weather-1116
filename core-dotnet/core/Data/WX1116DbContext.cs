global using UserEntity = Core.Data.Domain.User;
using Core.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace Core.Data;

public class WX1116DbContext(DbContextOptions<WX1116DbContext> options) : DbContext(options)
{
    public DbSet<AgentActivity> AgentActivities => Set<AgentActivity>();

    public DbSet<UserEntity> Users => Set<UserEntity>();

    public DbSet<UserCity> UserCities => Set<UserCity>();

    public DbSet<City> Cities => Set<City>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WX1116DbContext).Assembly);
    }
}
