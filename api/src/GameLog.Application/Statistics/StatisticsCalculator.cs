using GameLog.Domain.Enums;
using GameLog.Domain.Rules;

namespace GameLog.Application.Statistics;

/// <summary>Pure statistics maths over in-memory rows, so every number can be unit tested without a database.</summary>
public static class StatisticsCalculator
{
    public const int TopN = 5;

    public static StatisticsDto Compute(IReadOnlyCollection<DiaryStatRow> rows, int reviewCount, int collectionCount)
    {
        var byStatus = Enum.GetValues<DiaryStatus>().ToDictionary(s => s, s => rows.Count(r => r.Status == s));
        var rated = rows.Where(r => r.HalfStars is not null).ToList();

        var distribution = new int[RatingRules.MaxHalfStars];
        foreach (var r in rated)
        {
            distribution[r.HalfStars!.Value - 1]++;
        }

        double? average = rated.Count == 0 ? null : Math.Round(rated.Average(r => r.HalfStars!.Value) / 2.0, 2);

        return new StatisticsDto(
            rows.Count,
            byStatus,
            byStatus[DiaryStatus.Completed],
            (int)Math.Min(rows.Sum(r => (long)r.PlaytimeMinutes), int.MaxValue),
            average,
            rated.Count,
            distribution,
            Top(rows.SelectMany(r => r.Genres)),
            Top(rows.Where(r => r.Platform is not null).Select(r => r.Platform!)),
            rows.Where(r => r.PlaytimeMinutes > 0).OrderByDescending(r => r.PlaytimeMinutes).ThenBy(r => r.Title).Take(TopN).Select(ToStat).ToList(),
            rated.OrderByDescending(r => r.HalfStars).ThenByDescending(r => r.PlaytimeMinutes).ThenBy(r => r.Title).Take(TopN).Select(ToStat).ToList(),
            reviewCount,
            collectionCount);
    }

    /// <summary>Most frequent names first; ties break alphabetically so the output is deterministic.</summary>
    public static IReadOnlyList<NamedCount> Top(IEnumerable<string> names, int take = TopN) =>
        names.GroupBy(n => n).Select(g => new NamedCount(g.Key, g.Count()))
            .OrderByDescending(n => n.Count).ThenBy(n => n.Name, StringComparer.Ordinal).Take(take).ToList();

    public static GameStat ToStat(DiaryStatRow r) => new(r.GameId, r.Slug, r.Title, r.CoverUrl, r.PlaytimeMinutes, r.HalfStars);
}
