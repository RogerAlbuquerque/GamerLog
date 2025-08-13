using System.Globalization;
using System.Text;

namespace GameLog.Application.Common;

public static class SlugGenerator
{
    public const int MaxLength = 80;

    /// <summary>"Baldur's Gate 3" -> "baldurs-gate-3". Accents are removed; falls back to "game".</summary>
    public static string From(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "game";
        }

        var sb = new StringBuilder();
        var pendingDash = false;
        foreach (var ch in text.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (ch is '\'' or '’')
            {
                continue; // "Baldur's" -> "baldurs"
            }

            if (char.IsAsciiLetterOrDigit(ch))
            {
                if (pendingDash && sb.Length > 0)
                {
                    sb.Append('-');
                }

                sb.Append(char.ToLowerInvariant(ch));
                pendingDash = false;
            }
            else
            {
                pendingDash = true;
            }
        }

        var slug = sb.ToString();
        if (slug.Length > MaxLength)
        {
            slug = slug[..MaxLength].TrimEnd('-');
        }

        return slug.Length == 0 ? "game" : slug;
    }
}
