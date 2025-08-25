using System.Security.Claims;
using GameLog.Application.Abstractions;
using GameLog.Domain.Entities;
using GameLog.Domain.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GameLog.Api.Extensions;

public static class AuthExtensions
{
    public const string StampClaim = "gl:stamp";
    public const string AdminPolicy = "Admin";

    public static IServiceCollection AddGameLogAuthentication(this IServiceCollection services)
    {
        services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "gl.session";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.ExpireTimeSpan = TimeSpan.FromDays(14);
                options.SlidingExpiration = true;

                // An API answers with status codes, never redirects to a login page.
                options.Events.OnRedirectToLogin = ctx =>
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = ctx =>
                {
                    ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
                options.Events.OnValidatePrincipal = ValidateAgainstDatabaseAsync;
            });

        // Secure by default: every endpoint needs a signed-in user unless it opts out with [AllowAnonymous].
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(AdminPolicy, policy => policy.RequireRole(nameof(UserRole.Admin)));

        return services;
    }

    public static ClaimsPrincipal CreatePrincipal(User user)
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim(StampClaim, user.SecurityStamp.ToString()),
            ],
            CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    /// <summary>
    /// Every request re-checks the account: deleted users, changed passwords (new security stamp)
    /// and changed roles end the session immediately instead of at cookie expiry.
    /// </summary>
    private static async Task ValidateAgainstDatabaseAsync(CookieValidatePrincipalContext context)
    {
        var principal = context.Principal;
        var ok = Guid.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
                 & Guid.TryParse(principal?.FindFirstValue(StampClaim), out var stamp);

        if (ok)
        {
            var db = context.HttpContext.RequestServices.GetRequiredService<IAppDbContext>();
            var current = await db.ReadOnly<User>()
                .Where(u => u.Id == userId)
                .Select(u => new { u.SecurityStamp, u.Role })
                .FirstOrDefaultAsync(context.HttpContext.RequestAborted);
            ok = current is not null
                 && current.SecurityStamp == stamp
                 && current.Role.ToString() == principal!.FindFirstValue(ClaimTypes.Role);
        }

        if (!ok)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
