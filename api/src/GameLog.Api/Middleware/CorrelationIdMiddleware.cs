namespace GameLog.Api.Middleware;

/// <summary>
/// Gives every request a correlation id (echoed in the response and attached to every log
/// line). Client-supplied values are accepted only when they are short and alphanumeric,
/// which prevents log injection.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var id = context.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(id) || id.Length > 64 || !id.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'))
        {
            id = Guid.NewGuid().ToString("N");
        }

        context.TraceIdentifier = id;
        context.Response.Headers[HeaderName] = id;

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = id }))
        {
            await next(context);
        }
    }
}
