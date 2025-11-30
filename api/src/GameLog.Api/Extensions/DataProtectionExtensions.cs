using Microsoft.AspNetCore.DataProtection;

namespace GameLog.Api.Extensions;

public static class DataProtectionExtensions
{
    /// <summary>
    /// Session cookies and antiforgery tokens are encrypted with Data Protection keys. When
    /// <c>DataProtection:KeysPath</c> is set (the Docker image mounts a volume there) the key ring
    /// survives restarts, so users stay signed in across deployments.
    /// </summary>
    public static IServiceCollection AddGameLogDataProtection(this IServiceCollection services, IConfiguration configuration)
    {
        var path = configuration["DataProtection:KeysPath"];
        var builder = services.AddDataProtection().SetApplicationName("GameLog");
        if (!string.IsNullOrWhiteSpace(path))
        {
            Directory.CreateDirectory(path);
            builder.PersistKeysToFileSystem(new DirectoryInfo(path));
        }
        return services;
    }
}
