namespace GameLog.Domain.Rules;

public static class ReviewRules
{
    public const int MaxTitle = 120;
    public const int MinBody = 10;
    public const int MaxBody = 10000;
    public const int MaxExperience = 3000;
    public const int MaxProsCons = 1000;
    public const int MaxCharacter = 120;
    public const int MaxMoment = 500;
    public const int MaxPlaytimeMinutes = 6_000_000; // 100,000 hours

    public static IReadOnlyList<RuleViolation> Validate(
        string? title, string? body, string? experience, string? pros, string? cons,
        string? favoriteCharacter, string? favoriteMoment, int? playtimeMinutes)
    {
        var errors = new List<RuleViolation>();
        var t = title?.Trim();
        if (string.IsNullOrEmpty(t) || t.Length > MaxTitle)
        {
            errors.Add(new("title", $"Title is required and at most {MaxTitle} characters."));
        }

        var b = body?.Trim();
        if (string.IsNullOrEmpty(b) || b.Length < MinBody || b.Length > MaxBody)
        {
            errors.Add(new("body", $"The review must be {MinBody}-{MaxBody} characters."));
        }

        TooLong(errors, "experience", experience, MaxExperience);
        TooLong(errors, "pros", pros, MaxProsCons);
        TooLong(errors, "cons", cons, MaxProsCons);
        TooLong(errors, "favoriteCharacter", favoriteCharacter, MaxCharacter);
        TooLong(errors, "favoriteMoment", favoriteMoment, MaxMoment);

        if (playtimeMinutes is < 0 or > MaxPlaytimeMinutes)
        {
            errors.Add(new("playtimeMinutes", "Playtime must be a positive number of minutes."));
        }

        return errors;
    }

    /// <summary>Trims optional text; blank becomes null.</summary>
    public static string? Clean(string? value)
    {
        var v = value?.Trim();
        return string.IsNullOrEmpty(v) ? null : v;
    }

    private static void TooLong(List<RuleViolation> errors, string field, string? value, int max)
    {
        if (value is not null && value.Trim().Length > max)
        {
            errors.Add(new(field, $"At most {max} characters."));
        }
    }
}
