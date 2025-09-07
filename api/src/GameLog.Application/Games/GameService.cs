using GameLog.Application.Abstractions;
using GameLog.Application.Common;
using GameLog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameLog.Application.Games;

/// <summary>Read side of the catalog. Everything is served from the local database, never from a provider.</summary>
public sealed class GameService(IAppDbContext db)
{
    public const int MaxQueryLength = 100;

    private sealed record Row(
        Guid Id, string Slug, string Title, string? CoverKey, DateOnly? ReleaseDate, int RatingSum, int RatingCount,
        List<string> Genres, List<string> Platforms);

    public async Task<PagedResult<GameSummary>> SearchAsync(GameSearchQuery q, CancellationToken ct = default)
    {
        var games = db.ReadOnly<Game>();

        var term = Sanitize(q.Q);
        if (term.Length > 0)
        {
            var pattern = $"%{term}%";
            games = games.Where(g => EF.Functions.Like(g.Title.ToLower(), pattern));
        }

        if (!string.IsNullOrWhiteSpace(q.Genre))
        {
            var slug = q.Genre.Trim().ToLowerInvariant();
            games = games.Where(g => g.GameGenres.Any(x => x.Genre.Slug == slug));
        }

        if (!string.IsNullOrWhiteSpace(q.Platform))
        {
            var slug = q.Platform.Trim().ToLowerInvariant();
            games = games.Where(g => g.GamePlatforms.Any(x => x.Platform.Slug == slug));
        }

        if (q.Year is { } year)
        {
            var from = new DateOnly(Math.Clamp(year, 1950, 2100), 1, 1);
            var to = from.AddYears(1);
            games = games.Where(g => g.ReleaseDate >= from && g.ReleaseDate < to);
        }

        var ordered = (q.Sort?.ToLowerInvariant()) switch
        {
            GameSort.Title => games.OrderBy(g => g.Title).ThenBy(g => g.Id),
            GameSort.Release => games.OrderByDescending(g => g.ReleaseDate).ThenBy(g => g.Id),
            GameSort.Rating => games
                .OrderByDescending(g => g.RatingCount == 0 ? 0.0 : (double)g.RatingSum / g.RatingCount)
                .ThenByDescending(g => g.RatingCount).ThenBy(g => g.Id),
            _ => games.OrderByDescending(g => g.CompletedCount + g.RatingCount).ThenBy(g => g.Title).ThenBy(g => g.Id),
        };

        var page = await ordered
            .Select(g => new Row(
                g.Id, g.Slug, g.Title, g.CoverKey, g.ReleaseDate, g.RatingSum, g.RatingCount,
                g.GameGenres.Select(x => x.Genre.Name).OrderBy(n => n).ToList(),
                g.GamePlatforms.Select(x => x.Platform.Name).OrderBy(n => n).ToList()))
            .ToPagedAsync(q.Page, q.PageSize, ct);

        return new PagedResult<GameSummary>(page.Items.Select(ToSummary).ToList(), page.Page, page.PageSize, page.Total);
    }

    public async Task<GameDetail> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var key = (slug ?? "").Trim().ToLowerInvariant();
        var game = await db.ReadOnly<Game>()
            .Where(g => g.Slug == key)
            .Select(g => new
            {
                Game = g,
                Genres = g.GameGenres.Select(x => new NamedItem(x.Genre.Id, x.Genre.Name, x.Genre.Slug)).OrderBy(n => n.Name).ToList(),
                Platforms = g.GamePlatforms.Select(x => new NamedItem(x.Platform.Id, x.Platform.Name, x.Platform.Slug)).OrderBy(n => n.Name).ToList(),
            })
            .FirstOrDefaultAsync(ct) ?? throw new NotFoundException("Game not found.");

        var g = game.Game;
        return new GameDetail(
            g.Id, g.Slug, g.Title, g.Synopsis, MediaUrls.For(g.CoverKey), MediaUrls.For(g.BackgroundKey),
            g.ReleaseDate, g.Developer, g.Publisher, g.OfficialUrl, g.GameModes,
            game.Genres, game.Platforms, g.AverageRating, g.RatingCount, g.CompletedCount);
    }

    /// <summary>Games sharing the most genres with this one, ties broken by number of ratings.</summary>
    public async Task<IReadOnlyList<GameSummary>> RelatedAsync(string slug, int take = 8, CancellationToken ct = default)
    {
        var key = (slug ?? "").Trim().ToLowerInvariant();
        var source = await db.ReadOnly<Game>()
            .Where(g => g.Slug == key)
            .Select(g => new { g.Id, GenreIds = g.GameGenres.Select(x => x.GenreId).ToList() })
            .FirstOrDefaultAsync(ct) ?? throw new NotFoundException("Game not found.");

        if (source.GenreIds.Count == 0)
        {
            return [];
        }

        var genreIds = source.GenreIds;
        var rows = await db.ReadOnly<Game>()
            .Where(g => g.Id != source.Id && g.GameGenres.Any(x => genreIds.Contains(x.GenreId)))
            .OrderByDescending(g => g.GameGenres.Count(x => genreIds.Contains(x.GenreId)))
            .ThenByDescending(g => g.RatingCount)
            .ThenBy(g => g.Id)
            .Take(Math.Clamp(take, 1, 20))
            .Select(g => new Row(
                g.Id, g.Slug, g.Title, g.CoverKey, g.ReleaseDate, g.RatingSum, g.RatingCount,
                g.GameGenres.Select(x => x.Genre.Name).OrderBy(n => n).ToList(),
                g.GamePlatforms.Select(x => x.Platform.Name).OrderBy(n => n).ToList()))
            .ToListAsync(ct);

        return rows.Select(ToSummary).ToList();
    }

    public async Task<IReadOnlyList<NamedItem>> GenresAsync(CancellationToken ct = default) =>
        await db.ReadOnly<Genre>().OrderBy(x => x.Name).Select(x => new NamedItem(x.Id, x.Name, x.Slug)).ToListAsync(ct);

    public async Task<IReadOnlyList<NamedItem>> PlatformsAsync(CancellationToken ct = default) =>
        await db.ReadOnly<Platform>().OrderBy(x => x.Name).Select(x => new NamedItem(x.Id, x.Name, x.Slug)).ToListAsync(ct);

    /// <summary>LIKE wildcards in user input must be literal characters, not patterns.</summary>
    private static string Sanitize(string? term)
    {
        var t = (term ?? "").Trim().ToLowerInvariant();
        if (t.Length > MaxQueryLength)
        {
            t = t[..MaxQueryLength];
        }

        return t.Replace("%", "").Replace("_", "").Replace("\\", "");
    }

    private static GameSummary ToSummary(Row r) => new(
        r.Id, r.Slug, r.Title, MediaUrls.For(r.CoverKey), r.ReleaseDate,
        r.RatingCount == 0 ? null : Math.Round(r.RatingSum / 2.0 / r.RatingCount, 2),
        r.RatingCount, r.Genres, r.Platforms);
}
