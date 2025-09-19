using System.Text.RegularExpressions;

namespace GameLog.Domain.Rules;

public static partial class UserRules
{
    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin", "administrator", "api", "root", "system", "support", "gamelog", "me", "null", "undefined",
        "login", "logout", "register", "settings", "discover", "search", "games", "feed", "stats",
    };

    [GeneratedRegex("^[A-Za-z0-9_]{3,24}$")]
    private static partial Regex UsernamePattern();

    public static string NormalizeUsername(string username) => username.Trim().ToLowerInvariant();

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    /// <summary>Returns an error message, or null when the username is acceptable.</summary>
    public static string? ValidateUsername(string? username)
    {
        if (string.IsNullOrWhiteSpace(username) || !UsernamePattern().IsMatch(username.Trim()))
        {
            return "Username must be 3-24 characters: letters, digits or underscore.";
        }

        return Reserved.Contains(username.Trim()) ? "This username is reserved." : null;
    }

    /// <summary>Password policy: length over composition rules (NIST 800-63B style).</summary>
    public static string? ValidatePassword(string? password, string? username = null)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 10)
        {
            return "Password must be at least 10 characters.";
        }

        if (password.Length > 128)
        {
            return "Password must be at most 128 characters.";
        }

        if (username is not null && password.Contains(username, StringComparison.OrdinalIgnoreCase))
        {
            return "Password must not contain the username.";
        }

        return null;
    }

    /// <summary>Pragmatic email check: one @, a dotted domain, no whitespace, at most 254 characters.</summary>
    public static string? ValidateEmail(string? email)
    {
        const string message = "Enter a valid email address.";
        var value = email?.Trim();
        if (string.IsNullOrEmpty(value) || value.Length > 254 || value.Any(char.IsWhiteSpace))
        {
            return message;
        }

        var at = value.IndexOf('@');
        if (at < 1 || at != value.LastIndexOf('@') || at == value.Length - 1)
        {
            return message;
        }

        var domain = value[(at + 1)..];
        var ok = domain.Contains('.') && !domain.StartsWith('.') && !domain.EndsWith('.') && !domain.Contains("..");
        return ok ? null : message;
    }

    public const int MaxDisplayNameLength = 50;
    public const int MaxBioLength = 500;

    public static string? ValidateDisplayName(string? displayName)
    {
        var value = displayName?.Trim();
        if (string.IsNullOrEmpty(value))
        {
            return "Display name is required.";
        }

        if (value.Length > MaxDisplayNameLength)
        {
            return $"Display name must be at most {MaxDisplayNameLength} characters.";
        }

        return value.Any(char.IsControl) ? "Display name contains invalid characters." : null;
    }

    /// <summary>Bios may be empty (it clears the field) but are bounded and plain text.</summary>
    public static string? ValidateBio(string? bio)
    {
        if (bio is null)
        {
            return null;
        }

        if (bio.Length > MaxBioLength)
        {
            return $"Bio must be at most {MaxBioLength} characters.";
        }

        return bio.Any(c => char.IsControl(c) && c is not ('\n' or '\r')) ? "Bio contains invalid characters." : null;
    }
}
