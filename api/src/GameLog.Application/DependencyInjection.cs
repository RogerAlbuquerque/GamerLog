using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GameLog.Application;

public static class DependencyInjection
{
    /// <summary>Registers every concrete class named *Service in this assembly as a scoped service.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var serviceTypes = typeof(DependencyInjection).Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.Name.EndsWith("Service", StringComparison.Ordinal));

        foreach (var type in serviceTypes)
        {
            services.AddScoped(type);
        }

        services.TryAddSingleton(TimeProvider.System);
        return services;
    }
}
