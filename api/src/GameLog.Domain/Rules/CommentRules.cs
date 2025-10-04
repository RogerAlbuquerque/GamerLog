namespace GameLog.Domain.Rules;

public static class CommentRules
{
    public const int MaxBody = 2000;

    /// <summary>Returns the trimmed body, or an error message when the text is empty or too long.</summary>
    public static (string? Body, string? Error) Normalize(string? body)
    {
        var text = body?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return (null, "A comment cannot be empty.");
        }

        return text.Length > MaxBody ? (null, $"A comment can have at most {MaxBody} characters.") : (text, null);
    }
}
