using GameLog.Application.Abstractions;
using GameLog.Application.Catalog;
using GameLog.Infrastructure.Persistence;
using GameLog.Infrastructure.Providers.Igdb;
using GameLog.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GameLog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // Set ConnectionStrings__Default in the environment (see .env.example).
            throw new InvalidOperationException("ConnectionStrings:Default is not configured.");
        }

        services.AddDbContext<AppDbContext>(options => options
            .UseNpgsql(connectionString)
            // Migrations are hand-authored without a model snapshot (see docs/migrations.md).
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddGameLogStorage(configuration);
        services.AddIgdb(configuration);

        return services;
    }

    /// <summary>Object storage (S3-compatible by default), image processing and the artwork copier.</summary>
    public static IServiceCollection AddGameLogStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.Section));

        services.AddSingleton<IObjectStorage>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<StorageOptions>>();
            return options.Value.Provider.ToLowerInvariant() switch
            {
                "filesystem" => new FileSystemObjectStorage(options.Value.Path),
                "s3" => new S3ObjectStorage(options),
                var other => throw new InvalidOperationException($"Unknown Storage:Provider '{other}'. Use 's3' or 'filesystem'."),
            };
        });
        services.AddSingleton<IImageProcessor, ImageSharpProcessor>();

        services.AddHttpClient<IGameArtworkStore, HttpArtworkStore>(http => http.Timeout = TimeSpan.FromSeconds(15))
            // Redirects could bounce an allow-listed URL to an internal address.
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false })
            .AddStandardResilienceHandler();
        return services;
    }

    public static IServiceCollection AddIgdb(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<IgdbOptions>(configuration.GetSection(IgdbOptions.Section));
        services.AddHttpClient(IgdbGameProvider.HttpClientName, http => http.Timeout = TimeSpan.FromSeconds(20));
        services.AddSingleton<IGameMetadataProvider, IgdbGameProvider>();
        return services;
    }
}
