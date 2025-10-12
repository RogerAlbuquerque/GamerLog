namespace GameLog.Domain.Rules;

public static class CollectionRules
{
    public const int MaxName = 100;
    public const int MaxDescription = 1000;
    public const int MaxGames = 500;

    public static IReadOnlyList<RuleViolation> Validate(string? name, string? description)
    {
        var errors = new List<RuleViolation>();
        var n = name?.Trim();
        if (string.IsNullOrEmpty(n) || n.Length > MaxName)
        {
            errors.Add(new("name", $"Name is required and at most {MaxName} characters."));
        }

        if (description is not null && description.Trim().Length > MaxDescription)
        {
            errors.Add(new("description", $"Description can have at most {MaxDescription} characters."));
        }

        return errors;
    }

    /// <summary>
    /// True when <paramref name="requested"/> lists exactly the same games as <paramref name="current"/>, once each
    /// (a reorder may not add, drop or duplicate games).
    /// </summary>
    public static bool IsPermutation(IReadOnlyCollection<Guid> current, IReadOnlyCollection<Guid> requested) =>
        requested.Count == current.Count
        && requested.Distinct().Count() == requested.Count
        && !requested.Except(current).Any();
}
