using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace GameLog.Api.Extensions;

public static class RateLimitingExtensions
{
    public const string Auth = "auth";
    public const string Write = "write";
    public const string Upload = "upload";

    /// <summary>
    /// Fixed-window limits per caller (user id when signed in, otherwise client IP):
    /// a global ceiling, a strict policy for credential endpoints and one for uploads.
    /// <c>RateLimiting:Multiplier</c> scales every limit (integration tests raise it).
    /// </summary>
    public static IServiceCollection AddGameLogRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var m = Math.Max(configuration.GetValue("RateLimiting:Multiplier", 1), 1);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, ct) =>
            {
                context.HttpContext.Response.Headers["Retry-After"] = "60";
                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too many requests. Please slow down.",
                    Extensions = { ["code"] = "rate_limited" },
                };
                await context.HttpContext.Response.WriteAsJsonAsync(problem, ct);
            };

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                RateLimitPartition.GetFixedWindowLimiter("global:" + Caller(ctx), _ => Window(300 * m)));

            options.AddPolicy(Auth, ctx =>
                RateLimitPartition.GetFixedWindowLimiter("auth:" + Caller(ctx), _ => Window(10 * m)));
            options.AddPolicy(Write, ctx =>
                RateLimitPartition.GetFixedWindowLimiter("write:" + Caller(ctx), _ => Window(90 * m)));
            options.AddPolicy(Upload, ctx =>
                RateLimitPartition.GetFixedWindowLimiter("upload:" + Caller(ctx), _ => Window(10 * m)));
        });
        return services;
    }

    private static FixedWindowRateLimiterOptions Window(int permits) => new()
    {
        PermitLimit = permits,
        Window = TimeSpan.FromMinutes(1),
        QueueLimit = 0,
    };

    private static string Caller(HttpContext ctx) =>
        ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? ctx.Connection.RemoteIpAddress?.ToString()
        ?? "unknown";
}
