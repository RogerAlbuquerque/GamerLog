using Microsoft.AspNetCore.Mvc;

namespace GameLog.Api.Extensions;

public static class AntiforgeryExtensions
{
    public const string HeaderName = "X-CSRF-TOKEN";

    /// <summary>
    /// CSRF protection for cookie authentication: unsafe requests must echo the token from
    /// <c>GET /api/v1/auth/csrf</c> in the X-CSRF-TOKEN header. Tokens are bound to the user,
    /// so login and logout responses hand out a fresh one.
    /// </summary>
    public static IServiceCollection AddGameLogAntiforgery(this IServiceCollection services)
    {
        services.AddAntiforgery(options =>
        {
            options.HeaderName = HeaderName;
            options.Cookie.Name = "gl.csrf";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        });
        return services;
    }

    /// <summary>Applies antiforgery validation to every POST/PUT/PATCH/DELETE action (GET/HEAD/OPTIONS are skipped).</summary>
    public static MvcOptions AddAntiforgeryValidation(this MvcOptions options)
    {
        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
        return options;
    }
}
