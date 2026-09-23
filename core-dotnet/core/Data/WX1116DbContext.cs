global using UserEntity = Core.Data.Domain.User;
using Core.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace Core.Data;

public class WX1116DbContext(DbContextOptions<WX1116DbContext> options) : DbContext(options)
{
    public DbSet<AgentActivity> AgentActivity => Set<AgentActivity>();

    public DbSet<UserEntity> User => Set<UserEntity>();

    public DbSet<UserPin> UserPin => Set<UserPin>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WX1116DbContext).Assembly);
    }
}
