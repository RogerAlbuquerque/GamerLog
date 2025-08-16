using GameLog.Application.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GameLog.Api.Middleware;

/// <summary>
/// Converts exceptions into RFC 7807 problem details with a stable <c>code</c> extension.
/// Unexpected errors are logged with full detail but never leaked to the client.
/// </summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Client went away; nothing to write.
        }
        catch (Exception ex)
        {
            await WriteProblemAsync(context, ex);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception ex)
    {
        if (context.Response.HasStarted)
        {
            logger.LogError(ex, "Exception after the response started");
            throw ex is OperationCanceledException ? ex : new InvalidOperationException("Response already started.", ex);
        }

        var problem = ex switch
        {
            ValidationFailedException v => new ValidationProblemDetails(v.Errors.ToDictionary(e => e.Key, e => e.Value))
            {
                Status = v.Status,
                Title = v.Message,
                Extensions = { ["code"] = v.Code },
            },
            AppException a => new ProblemDetails { Status = a.Status, Title = a.Message, Extensions = { ["code"] = a.Code } },
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } } =>
                new ProblemDetails { Status = 409, Title = "The resource already exists.", Extensions = { ["code"] = "conflict" } },
            _ => null,
        };

        if (problem is null)
        {
            logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            problem = new ProblemDetails
            {
                Status = 500,
                Title = "An unexpected error occurred.",
                Extensions = { ["code"] = "internal_error" },
            };
        }
        else
        {
            logger.LogInformation("Handled {Code} for {Method} {Path}", problem.Extensions["code"], context.Request.Method, context.Request.Path);
        }

        problem.Extensions["traceId"] = context.TraceIdentifier;
        context.Response.Clear();
        context.Response.StatusCode = problem.Status ?? 500;
        await context.Response.WriteAsJsonAsync(problem, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json");
    }
}
