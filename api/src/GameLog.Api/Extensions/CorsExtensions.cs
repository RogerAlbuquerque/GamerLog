namespace GameLog.Api.Extensions;

public static class CorsExtensions
{
    /// <summary>
    /// Explicit origin allow-list from <c>Cors:AllowedOrigins</c> (comma separated in the
    /// <c>ALLOWED_ORIGINS</c> variable). No wildcard is ever used because credentials are allowed.
    /// </summary>
    public static IServiceCollection AddGameLogCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        var fromEnv = configuration["Cors:AllowedOriginsCsv"];
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            origins = [.. origins, .. fromEnv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
        }

        services.AddCors(options => options.AddDefaultPolicy(policy =>
        {
            if (origins.Length == 0)
            {
                return; // same-origin only
            }

            policy.WithOrigins(origins)
                .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
                .WithHeaders("Content-Type", AntiforgeryExtensions.HeaderName)
                .AllowCredentials();
        }));
        return services;
    }
}
