using GameLog.Application.Abstractions;
using GameLog.Application.Common;
using GameLog.Application.Games;
using GameLog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameLog.Application.Discovery;

/// <summary>Home-page rails and personalised suggestions. Everything is computed from our own data, no external calls.</summary>
public sealed class DiscoveryService(IAppDbContext db, TimeProvider clock)
{
    public const int RailSize = 12;
    public const int TrendingDays = 14;
    public const int NewReleaseDays = 120;
    public const int CandidatePool = 300;

    // Bayesian average: a prior of 5 ratings worth 3.5 stars keeps a game with a single 5-star vote from topping the chart.
    private const double PriorWeight = 5.0;
    private const double PriorStars = 3.5;

    private sealed record Row(
        Guid Id, string Slug, string Title, string? CoverKey, DateOnly? ReleaseDate, int RatingSum, int RatingCount,
        List<string> Genres, List<string> Platforms);

    private static IQueryable<Row> Project(IQueryable<Game> q) => q.Select(g => new Row(
        g.Id, g.Slug, g.Title, g.CoverKey, g.ReleaseDate, g.RatingSum, g.RatingCount,
        g.GameGenres.Select(x => x.Genre.Name).OrderBy(n => n).ToList(),
        g.GamePlatforms.Select(x => x.Platform.Name).OrderBy(n => n).ToList()));

    private static GameSummary ToSummary(Row r) => new(
        r.Id, r.Slug, r.Title, MediaUrls.For(r.CoverKey), r.ReleaseDate,
        r.RatingCount == 0 ? null : Math.Round(r.RatingSum / 2.0 / r.RatingCount, 2), r.RatingCount, r.Genres, r.Platforms);

    private async Task<IReadOnlyList<GameSummary>> RailAsync(IQueryable<Game> ordered, CancellationToken ct) =>
        (await Project(ordered.Take(RailSize)).ToListAsync(ct)).Select(ToSummary).ToList();

    public async Task<DiscoveryDto> OverviewAsync(CancellationToken ct = default)
    {
        var games = db.ReadOnly<Game>();
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var since = today.AddDays(-NewReleaseDays);

        var popular = await RailAsync(games.OrderByDescending(g => g.RatingCount + g.CompletedCount).ThenBy(g => g.Title).ThenBy(g => g.Id), ct);
        var topRated = await RailAsync(
            games.Where(g => g.RatingCount > 0)
                .OrderByDescending(g => (g.RatingSum / 2.0 + PriorWeight * PriorStars) / (g.RatingCount + PriorWeight))
                .ThenByDescending(g => g.RatingCount).ThenBy(g => g.Title).ThenBy(g => g.Id), ct);
        var newReleases = await RailAsync(
            games.Where(g => g.ReleaseDate != null && g.ReleaseDate >= since && g.ReleaseDate <= today)
                .OrderByDescending(g => g.ReleaseDate).ThenBy(g => g.Title).ThenBy(g => g.Id), ct);
        var mostCompleted = await RailAsync(
            games.Where(g => g.CompletedCount > 0).OrderByDescending(g => g.CompletedCount).ThenBy(g => g.Title).ThenBy(g => g.Id), ct);

        return new DiscoveryDto(popular, topRated, newReleases, mostCompleted, await TrendingAsync(today, ct));
    }

    /// <summary>Games added to the most diaries over the last two weeks.</summary>
    private async Task<IReadOnlyList<GameSummary>> TrendingAsync(DateOnly today, CancellationToken ct)
    {
        var cutoff = today.AddDays(-TrendingDays).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var hot = await db.ReadOnly<GamingDiaryEntry>().Where(e => e.CreatedAt >= cutoff)
            .GroupBy(e => e.GameId).Select(g => new { GameId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count).ThenBy(x => x.GameId).Take(RailSize).ToListAsync(ct);
        if (hot.Count == 0)
        {
            return [];
        }

        var ids = hot.Select(h => h.GameId).ToList();
        var rows = await Project(db.ReadOnly<Game>().Where(g => ids.Contains(g.Id))).ToListAsync(ct);
        var rank = hot.Select((h, i) => (h.GameId, i)).ToDictionary(x => x.GameId, x => x.i);
        return rows.OrderBy(r => rank[r.Id]).Select(ToSummary).ToList();
    }

    /// <summary>Suggestions from the caller's own taste. Games already in their diary are never suggested.</summary>
    public async Task<IReadOnlyList<RecommendedGameDto>> RecommendAsync(Guid userId, int take = RailSize, CancellationToken ct = default)
    {
        var ratings = db.ReadOnly<Rating>().Where(r => r.UserId == userId);
        var diary = await db.ReadOnly<GamingDiaryEntry>().Where(e => e.UserId == userId)
            .Select(e => new
            {
                e.GameId, e.Status,
                HalfStars = ratings.Where(r => r.GameId == e.GameId).Select(r => (int?)r.HalfStars).FirstOrDefault(),
                Genres = e.Game.GameGenres.Select(x => x.Genre.Name).ToList(),
                Platforms = e.Game.GamePlatforms.Select(x => x.Platform.Name).ToList(),
            }).ToListAsync(ct);

        var signals = diary.Select(d => new TasteSignal(d.Status, d.HalfStars, d.Genres, d.Platforms)).ToList();
        var owned = diary.Select(d => d.GameId).ToList();

        var pool = await Project(db.ReadOnly<Game>().Where(g => !owned.Contains(g.Id))
                .OrderByDescending(g => g.RatingCount + g.CompletedCount).ThenBy(g => g.Title).ThenBy(g => g.Id).Take(CandidatePool))
            .ToListAsync(ct);

        var candidates = pool.Select(r => new Candidate(
            r.Id, r.Title, r.Genres, r.Platforms, r.RatingCount == 0 ? null : r.RatingSum / 2.0 / r.RatingCount, r.RatingCount));
        var byId = pool.ToDictionary(r => r.Id);
        return Recommender.Rank(signals, candidates, Math.Clamp(take, 1, 30))
            .Select(rec => new RecommendedGameDto(ToSummary(byId[rec.GameId]), rec.Score, rec.Because)).ToList();
    }
}
