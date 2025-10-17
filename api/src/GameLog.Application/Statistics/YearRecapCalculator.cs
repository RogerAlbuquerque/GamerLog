using GameLog.Domain.Enums;

namespace GameLog.Application.Statistics;

/// <summary>
/// The yearly retrospective ("Wrapped"). A game belongs to a year when one of its playthroughs was completed
/// in that year, so replaying a game in a later year counts again there. Pure, hence fully unit testable.
/// </summary>
public static class YearRecapCalculator
{
    private sealed record Completion(DiaryStatRow Row, PlaythroughStat Run);

    public static YearRecapDto Compute(
        int year, IReadOnlyCollection<DiaryStatRow> rows, int reviewsWritten, int ratingsGiven)
    {
        var completions = rows
            .Where(r => r.Status == DiaryStatus.Completed || r.Playthroughs.Any(p => p.CompletedOn != null))
            .SelectMany(r => r.Playthroughs.Where(p => p.CompletedOn?.Year == year).Select(p => new Completion(r, p)))
            .OrderBy(c => c.Run.CompletedOn).ThenBy(c => c.Row.Title, StringComparer.Ordinal)
            .ToList();

        var distinct = completions.Select(c => c.Row).DistinctBy(r => r.GameId).ToList();
        var byMonth = new int[12];
        foreach (var c in completions)
        {
            byMonth[c.Run.CompletedOn!.Value.Month - 1]++;
        }

        var max = byMonth.Max();
        int? busiest = max == 0 ? null : Array.IndexOf(byMonth, max) + 1;

        var rated = distinct.Where(r => r.HalfStars is not null).ToList();
        double? average = rated.Count == 0 ? null : Math.Round(rated.Average(r => r.HalfStars!.Value) / 2.0, 2);

        RecapGameDto? Pick(Completion? c) => c is null
            ? null
            : new RecapGameDto(c.Row.GameId, c.Row.Slug, c.Row.Title, c.Row.CoverUrl, c.Run.PlaytimeMinutes, c.Row.HalfStars, c.Run.CompletedOn);

        var timed = completions.Where(c => c.Run.PlaytimeMinutes > 0).ToList();
        var years = rows.SelectMany(r => r.Playthroughs).Where(p => p.CompletedOn != null).Select(p => p.CompletedOn!.Value.Year)
            .Distinct().OrderByDescending(y => y).ToList();

        return new YearRecapDto(
            year,
            distinct.Count,
            completions.Count,
            (int)Math.Min(completions.Sum(c => (long)c.Run.PlaytimeMinutes), int.MaxValue),
            average,
            byMonth,
            busiest,
            StatisticsCalculator.Top(distinct.SelectMany(r => r.Genres)),
            StatisticsCalculator.Top(distinct.Where(r => r.Platform is not null).Select(r => r.Platform!)),
            Pick(timed.OrderByDescending(c => c.Run.PlaytimeMinutes).ThenBy(c => c.Row.Title, StringComparer.Ordinal).FirstOrDefault()),
            Pick(timed.OrderBy(c => c.Run.PlaytimeMinutes).ThenBy(c => c.Row.Title, StringComparer.Ordinal).FirstOrDefault()),
            Pick(completions.FirstOrDefault()),
            Pick(completions.LastOrDefault()),
            completions.Where(c => c.Row.HalfStars is not null).DistinctBy(c => c.Row.GameId)
                .OrderByDescending(c => c.Row.HalfStars).ThenByDescending(c => c.Run.PlaytimeMinutes).ThenBy(c => c.Row.Title, StringComparer.Ordinal)
                .Take(3).Select(c => Pick(c)!).ToList(),
            reviewsWritten,
            ratingsGiven,
            years);
    }
}
