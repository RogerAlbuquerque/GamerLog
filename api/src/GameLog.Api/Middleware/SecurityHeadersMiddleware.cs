namespace GameLog.Api.Middleware;

/// <summary>Defensive response headers for an API that only ever returns JSON or images.</summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";

        var path = context.Request.Path;
        if (!path.StartsWithSegments("/swagger"))
        {
            // Swagger UI needs inline scripts; the API itself never serves HTML.
            headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'";
        }

        // Authenticated JSON must never be stored by shared caches; media is immutable and cacheable.
        if (path.StartsWithSegments("/api") && !path.StartsWithSegments("/api/v1/media"))
        {
            headers["Cache-Control"] = "no-store";
        }

        return next(context);
    }
}
