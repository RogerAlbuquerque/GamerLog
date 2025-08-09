using GameLog.Domain.Rules;

namespace GameLog.Application.Common;

/// <summary>Base for expected, client-facing failures. The API maps these to RFC 7807 responses.</summary>
public abstract class AppException(int status, string code, string message) : Exception(message)
{
    public int Status { get; } = status;
    public string Code { get; } = code;
}

public sealed class NotFoundException(string message = "The requested resource was not found.")
    : AppException(404, "not_found", message)
{
}

public sealed class ForbiddenException(string message = "You are not allowed to perform this action.")
    : AppException(403, "forbidden", message)
{
}

public sealed class UnauthorizedException(string message = "Authentication is required.")
    : AppException(401, "unauthorized", message)
{
}

public sealed class ConflictException(string code, string message) : AppException(409, code, message)
{
}

public sealed class ValidationFailedException : AppException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationFailedException(IReadOnlyDictionary<string, string[]> errors)
        : base(400, "validation_failed", "One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public ValidationFailedException(string field, string message)
        : this(new Dictionary<string, string[]> { [field] = [message] })
    {
    }

    public static ValidationFailedException From(IEnumerable<RuleViolation> violations) =>
        new(violations
            .GroupBy(v => v.Field)
            .ToDictionary(g => g.Key, g => g.Select(v => v.Message).ToArray()));
}
