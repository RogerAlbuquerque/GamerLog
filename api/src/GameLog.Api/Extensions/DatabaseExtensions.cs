using GameLog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GameLog.Api.Extensions;

public static class DatabaseExtensions
{
    /// <summary>Applies pending migrations at start-up when <c>Database:AutoMigrate</c> is true.</summary>
    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue("Database:AutoMigrate", false))
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Database");
        logger.LogInformation("Applying database migrations");
        await db.Database.MigrateAsync();
    }
}
