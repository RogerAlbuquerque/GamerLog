using GameLog.Domain.Enums;

namespace GameLog.Application.Discovery;

/// <summary>What the recommender knows about one game the user has in their diary.</summary>
public sealed record TasteSignal(DiaryStatus Status, int? HalfStars, IReadOnlyList<string> Genres, IReadOnlyList<string> Platforms);

public sealed record Candidate(
    Guid GameId, string Title, IReadOnlyList<string> Genres, IReadOnlyList<string> Platforms, double? AverageRating, int RatingCount);

public sealed record Recommendation(Guid GameId, double Score, IReadOnlyList<string> Because);

/// <summary>
/// Transparent, rule-based recommendations (no ML): genres the user completes and rates highly pull matching games
/// up, genres they abandon or rate low push them down, and well-rated games get a small quality bonus. Because
/// the rules are explicit, every suggestion can explain itself ("because you like RPG").
/// </summary>
public static class Recommender
{
    public const double QualityWeight = 1.0;
    public const double PlatformWeight = 0.5;
    public const int MinRatingsForFullQuality = 20;

    /// <summary>How much one diary entry says about the user's taste: status plus an optional rating around the 3-star midpoint.</summary>
    public static double SignalStrength(TasteSignal s)
    {
        var status = s.Status switch
        {
            DiaryStatus.Completed => 1.0,
            DiaryStatus.Playing => 0.7,
            DiaryStatus.WantToPlay => 0.4,
            DiaryStatus.OnHold => 0.2,
            DiaryStatus.Abandoned => -0.5,
            _ => 0,
        };
        var rating = s.HalfStars is { } h ? (h - 6) / 4.0 * 1.5 : 0;
        return status + rating;
    }

    public static IReadOnlyDictionary<string, double> BuildGenreProfile(IEnumerable<TasteSignal> signals)
    {
        var profile = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var s in signals)
        {
            var strength = SignalStrength(s);
            foreach (var genre in s.Genres)
            {
                profile[genre] = profile.GetValueOrDefault(genre) + strength;
            }
        }

        return profile;
    }

    public static IReadOnlyDictionary<string, double> BuildPlatformProfile(IEnumerable<TasteSignal> signals)
    {
        var profile = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var s in signals.Where(x => SignalStrength(x) > 0))
        {
            foreach (var platform in s.Platforms)
            {
                profile[platform] = profile.GetValueOrDefault(platform) + 1;
            }
        }

        return profile;
    }

    public static IReadOnlyList<Recommendation> Rank(
        IReadOnlyCollection<TasteSignal> signals, IEnumerable<Candidate> candidates, int take = 12)
    {
        var genres = BuildGenreProfile(signals);
        var platforms = BuildPlatformProfile(signals);
        var maxGenre = genres.Values.DefaultIfEmpty(0).Max();
        var maxPlatform = platforms.Values.DefaultIfEmpty(0).Max();

        var scored = new List<(Candidate Game, Recommendation Result)>();
        foreach (var c in candidates)
        {
            // Normalised by the user's strongest genre so scores do not grow with diary size, and by the
            // candidate's genre count so tagging a game with many genres does not inflate its score.
            var genreScore = maxGenre <= 0 || c.Genres.Count == 0
                ? 0
                : c.Genres.Sum(g => genres.GetValueOrDefault(g)) / maxGenre / Math.Sqrt(c.Genres.Count);
            var platformScore = maxPlatform <= 0 ? 0 : c.Platforms.Select(p => platforms.GetValueOrDefault(p)).DefaultIfEmpty(0).Max() / maxPlatform;
            var quality = c.AverageRating is { } avg
                ? avg / 5.0 * Math.Min(1.0, c.RatingCount / (double)MinRatingsForFullQuality)
                : 0;

            var score = genreScore + PlatformWeight * platformScore + QualityWeight * quality;
            var because = c.Genres.Where(g => genres.GetValueOrDefault(g) > 0).OrderByDescending(g => genres[g]).ThenBy(g => g, StringComparer.Ordinal).Take(2).ToList();
            scored.Add((c, new Recommendation(c.GameId, Math.Round(score, 4), because)));
        }

        return scored
            .OrderByDescending(x => x.Result.Score).ThenBy(x => x.Game.Title, StringComparer.Ordinal).ThenBy(x => x.Game.GameId)
            .Take(take).Select(x => x.Result).ToList();
    }
}
