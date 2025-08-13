using GameLog.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GameLog.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Every IEntityTypeConfiguration<T> in this assembly is picked up automatically.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
