namespace GameLog.Domain.Rules;

/// <summary>Ratings are stored as integer half-stars (1-10) to avoid floating-point drift.</summary>
public static class RatingRules
{
    public const int MinHalfStars = 1;
    public const int MaxHalfStars = 10;

    public static bool IsValid(int halfStars) => halfStars is >= MinHalfStars and <= MaxHalfStars;

    public static double ToStars(int halfStars) => halfStars / 2.0;

    /// <summary>Converts 0.5-5.0 stars into half-stars; returns null for values that are not a multiple of 0.5.</summary>
    public static int? FromStars(double stars)
    {
        var halves = stars * 2;
        if (Math.Abs(halves - Math.Round(halves)) > 1e-9)
        {
            return null;
        }

        var value = (int)Math.Round(halves);
        return IsValid(value) ? value : null;
    }
}
