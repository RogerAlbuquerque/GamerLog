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
}
