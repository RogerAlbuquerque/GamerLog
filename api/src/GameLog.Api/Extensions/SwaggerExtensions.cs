using Microsoft.OpenApi.Models;

namespace GameLog.Api.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddGameLogSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "GameLog API",
                Version = "v1",
                Description = "REST API of the GameLog social gaming journal. " +
                              "Unsafe requests need the X-CSRF-TOKEN header returned by GET /api/v1/auth/csrf.",
            });
        });
        return services;
    }

    /// <summary>Swagger UI is on in Development, or anywhere when Swagger:Enabled is true.</summary>
    public static WebApplication UseGameLogSwagger(this WebApplication app)
    {
        if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
        {
            app.UseSwagger();
            app.UseSwaggerUI(options => options.DocumentTitle = "GameLog API");
        }

        return app;
    }
}
