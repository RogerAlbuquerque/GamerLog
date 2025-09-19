using GameLog.Application.Abstractions;
using GameLog.Application.Auth;
using GameLog.Application.Catalog;
using GameLog.Domain.Entities;
using GameLog.Domain.Enums;
using GameLog.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;

namespace GameLog.Api.Extensions;

public static class SeedExtensions
{
    /// <summary>
    /// Optional start-up data. <c>Seed:Catalog</c> loads the built-in games into an empty catalog;
    /// <c>Seed:AdminUsername/AdminEmail/AdminPassword</c> create the first administrator when none exists.
    /// Nothing runs unless it is switched on, and no default credentials exist anywhere in the code.
    /// </summary>
    public static async Task SeedDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Seed");
        var db = sp.GetRequiredService<IAppDbContext>();
        var config = app.Configuration.GetSection("Seed");

        if (config.GetValue("Catalog", false) && !await db.ReadOnly<Game>().AnyAsync())
        {
            var import = sp.GetRequiredService<GameImportService>();
            var games = SeedCatalog.Load();
            foreach (var game in games)
            {
                await import.UpsertAsync(game);
            }

            logger.LogInformation("Seeded {Count} games into the catalog", games.Count);
        }

        var username = config["AdminUsername"];
        var email = config["AdminEmail"];
        var password = config["AdminPassword"];
        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password)
            && !await db.ReadOnly<User>().AnyAsync(u => u.Role == UserRole.Admin))
        {
            var auth = sp.GetRequiredService<AuthService>();
            var user = await auth.RegisterAsync(new RegisterRequest(username, email, password, "Administrator"));
            var tracked = await db.Query<User>().SingleAsync(u => u.Id == user.Id);
            tracked.Role = UserRole.Admin;
            await db.SaveChangesAsync();
            logger.LogInformation("Created the administrator account {Username}", username);
        }
    }
}
