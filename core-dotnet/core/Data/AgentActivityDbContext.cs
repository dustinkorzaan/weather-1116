using Core.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace Core.Data;

public class AgentActivityDbContext(DbContextOptions<AgentActivityDbContext> options) : DbContext(options)
{
    public DbSet<AgentActivity> AgentActivity => Set<AgentActivity>();

    public DbSet<User> User => Set<User>();

    public DbSet<UserPin> UserPin => Set<UserPin>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AgentActivityDbContext).Assembly);
    }
}
