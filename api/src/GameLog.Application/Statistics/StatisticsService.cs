using GameLog.Application.Abstractions;
using GameLog.Application.Common;
using GameLog.Domain.Entities;
using GameLog.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace GameLog.Application.Statistics;

/// <summary>Loads a user's diary once and hands it to the pure calculators. Diaries are public, so statistics are too.</summary>
public sealed class StatisticsService(IAppDbContext db)
{
    private async Task<Guid> ResolveAsync(string username, CancellationToken ct)
    {
        var normalized = UserRules.NormalizeUsername(username);
        return await db.ReadOnly<User>().Where(u => u.UsernameNormalized == normalized).Select(u => (Guid?)u.Id).FirstOrDefaultAsync(ct)
               ?? throw new NotFoundException("User not found.");
    }

    private async Task<List<DiaryStatRow>> LoadRowsAsync(Guid userId, CancellationToken ct)
    {
        var ratings = db.ReadOnly<Rating>().Where(r => r.UserId == userId);
        var entries = await db.ReadOnly<GamingDiaryEntry>().Where(e => e.UserId == userId)
            .Select(e => new
            {
                e.GameId, e.Game.Slug, e.Game.Title, e.Game.CoverKey, e.Status, e.TotalPlaytimeMinutes,
                Platform = e.Platform != null ? e.Platform.Name : null,
                Genres = e.Game.GameGenres.Select(g => g.Genre.Name).ToList(),
                Runs = e.Playthroughs.Select(p => new { p.CompletedOn, p.PlaytimeMinutes }).ToList(),
                HalfStars = ratings.Where(r => r.GameId == e.GameId).Select(r => (int?)r.HalfStars).FirstOrDefault(),
                ReleaseYear = e.Game.ReleaseDate != null ? (int?)e.Game.ReleaseDate.Value.Year : null,
            })
            .ToListAsync(ct);

        return entries.Select(e => new DiaryStatRow(
            e.GameId, e.Slug, e.Title, MediaUrls.For(e.CoverKey), e.Status, e.TotalPlaytimeMinutes, e.Platform, e.Genres,
            e.Runs.Select(p => new PlaythroughStat(p.CompletedOn, p.PlaytimeMinutes)).ToList(), e.HalfStars, e.ReleaseYear)).ToList();
    }

    public async Task<StatisticsDto> GetAsync(string username, CancellationToken ct = default)
    {
        var userId = await ResolveAsync(username, ct);
        var rows = await LoadRowsAsync(userId, ct);
        var reviews = await db.ReadOnly<Review>().CountAsync(r => r.UserId == userId, ct);
        var collections = await db.ReadOnly<GameCollection>().CountAsync(c => c.UserId == userId && c.Visibility == Domain.Enums.Visibility.Public, ct);
        return StatisticsCalculator.Compute(rows, reviews, collections);
    }

    public async Task<YearRecapDto> RecapAsync(string username, int year, CancellationToken ct = default)
    {
        if (year is < 1970 or > 2200)
        {
            throw new ValidationFailedException("year", "Enter a valid year.");
        }

        var userId = await ResolveAsync(username, ct);
        var rows = await LoadRowsAsync(userId, ct);
        var reviews = await db.ReadOnly<Review>().CountAsync(r => r.UserId == userId && r.CreatedAt.Year == year, ct);
        var ratings = await db.ReadOnly<Rating>().CountAsync(r => r.UserId == userId && r.CreatedAt.Year == year, ct);
        return YearRecapCalculator.Compute(year, rows, reviews, ratings);
    }
}
