using GameLog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace GameLog.Tests.Integration;

/// <summary>
/// Boots the real application (middleware, auth, antiforgery, rate limiting, controllers) in memory.
/// Only the database provider is swapped: PostgreSQL becomes SQLite so no external service is required.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection connection = new("DataSource=:memory:");
    private readonly string storagePath = Path.Combine(Path.GetTempPath(), "gamelog-it-" + Guid.NewGuid().ToString("N"));

    public ApiFactory()
    {
        connection.Open();
        // The schema must exist before the host starts: Program seeds the catalog during start-up.
        using (var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options))
        {
            db.Database.EnsureCreated();
        }

        // Secure cookies are only sent back over https.
        ClientOptions.BaseAddress = new Uri("https://localhost");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", "Host=unused;Database=unused");
        builder.UseSetting("Storage:Provider", "filesystem");
        builder.UseSetting("Storage:Path", storagePath);
        builder.UseSetting("Seed:Catalog", "true");
        builder.UseSetting("RateLimiting:Multiplier", "1000");
        builder.UseSetting("Cors:AllowedOriginsCsv", "https://app.example.test");

        builder.ConfigureServices(services =>
        {
            foreach (var d in services.Where(d =>
                         d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                         || d.ServiceType == typeof(IDbContextOptionsConfiguration<AppDbContext>)).ToList())
            {
                services.Remove(d);
            }

            services.AddDbContext<AppDbContext>(o => o.UseSqlite(connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            connection.Dispose();
            if (Directory.Exists(storagePath))
            {
                Directory.Delete(storagePath, true);
            }
        }
    }
}
