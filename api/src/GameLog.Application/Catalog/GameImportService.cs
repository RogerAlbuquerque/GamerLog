using GameLog.Application.Abstractions;
using GameLog.Application.Common;
using GameLog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameLog.Application.Catalog;

public sealed record ImportedGame(Guid Id, string Slug, string Title, bool Created);

/// <summary>
/// Copies games from an external provider into the local catalog. Imports are idempotent:
/// the same (provider, external id) always updates the same row.
/// </summary>
public sealed class GameImportService(
    IAppDbContext db,
    IEnumerable<IGameMetadataProvider> providers,
    IGameArtworkStore artwork,
    TimeProvider clock,
    ILogger<GameImportService> logger)
{
    private const int MaxGameModesLength = 300;

    private IGameMetadataProvider Provider =>
        providers.FirstOrDefault() ?? throw new ProviderUnavailableException("No game database provider is configured.");

    public async Task<IReadOnlyList<ExternalGame>> SearchProviderAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
        {
            throw new ValidationFailedException("q", "Enter at least 2 characters.");
        }

        return await Provider.SearchAsync(query.Trim(), 10, ct);
    }

    public async Task<ImportedGame> ImportAsync(string externalId, CancellationToken ct = default)
    {
        var external = await Provider.GetAsync(externalId?.Trim() ?? "", ct)
                       ?? throw new NotFoundException("The provider has no game with this id.");
        return await UpsertAsync(external, ct);
    }

    public async Task<ImportedGame> UpsertAsync(ExternalGame ext, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var game = await db.Query<Game>()
            .Include(g => g.GameGenres).ThenInclude(x => x.Genre)
            .Include(g => g.GamePlatforms).ThenInclude(x => x.Platform)
            .FirstOrDefaultAsync(g => g.ExternalProvider == ext.Provider && g.ExternalId == ext.ExternalId, ct);

        var created = game is null;
        if (game is null)
        {
            game = new Game
            {
                ExternalProvider = ext.Provider,
                ExternalId = ext.ExternalId,
                Slug = await UniqueSlugAsync(ext.Title, ct),
                CreatedAt = now,
            };
            db.Set<Game>().Add(game);
        }

        game.Title = ext.Title;
        game.Synopsis = ext.Summary;
        game.ReleaseDate = ext.ReleaseDate;
        game.Developer = ext.Developer;
        game.Publisher = ext.Publisher;
        game.OfficialUrl = ext.WebsiteUrl;
        game.GameModes = FitModes(ext.GameModes);
        game.UpdatedAt = now;

        // Artwork is copied only once; a provider hiccup must not fail the import.
        game.CoverKey ??= await artwork.StoreAsync(ext.CoverUrl, ImageKind.Cover, ct);
        game.BackgroundKey ??= await artwork.StoreAsync(ext.BackgroundUrl, ImageKind.Background, ct);

        await SyncGenresAsync(game, ext.Genres, ct);
        await SyncPlatformsAsync(game, ext.Platforms, ct);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "Import of {Provider}:{ExternalId} conflicted with a concurrent write", ext.Provider, ext.ExternalId);
            throw new ConflictException("import_conflict", "This game is being imported by someone else. Try again.");
        }

        logger.LogInformation("{Action} game {Title} ({Provider}:{ExternalId})", created ? "Imported" : "Updated", game.Title, ext.Provider, ext.ExternalId);
        return new ImportedGame(game.Id, game.Slug, game.Title, created);
    }

    private async Task<string> UniqueSlugAsync(string title, CancellationToken ct)
    {
        var baseSlug = SlugGenerator.From(title);
        var slug = baseSlug;
        for (var n = 2; await db.Query<Game>().AnyAsync(g => g.Slug == slug, ct); n++)
        {
            slug = $"{baseSlug}-{n}";
        }

        return slug;
    }

    /// <summary>Makes the game's genres exactly the wanted names, creating missing genres on the way.</summary>
    private async Task SyncGenresAsync(Game game, IReadOnlyList<string> names, CancellationToken ct)
    {
        var wanted = names.GroupBy(SlugGenerator.From).ToDictionary(g => g.Key, g => g.First());
        game.GameGenres.RemoveAll(x => !wanted.ContainsKey(x.Genre.Slug));
        var have = game.GameGenres.Select(x => x.Genre.Slug).ToHashSet();
        var missing = wanted.Where(w => !have.Contains(w.Key)).ToList();
        if (missing.Count == 0)
        {
            return;
        }

        var slugs = missing.Select(m => m.Key).ToList();
        var existing = await db.Query<Genre>().Where(g => slugs.Contains(g.Slug)).ToListAsync(ct);
        foreach (var (slug, name) in missing)
        {
            var genre = existing.FirstOrDefault(g => g.Slug == slug);
            if (genre is null)
            {
                genre = new Genre { Name = name, Slug = slug };
                db.Set<Genre>().Add(genre);
            }

            game.GameGenres.Add(new GameGenre { Genre = genre });
        }
    }

    private async Task SyncPlatformsAsync(Game game, IReadOnlyList<string> names, CancellationToken ct)
    {
        var wanted = names.GroupBy(SlugGenerator.From).ToDictionary(g => g.Key, g => g.First());
        game.GamePlatforms.RemoveAll(x => !wanted.ContainsKey(x.Platform.Slug));
        var have = game.GamePlatforms.Select(x => x.Platform.Slug).ToHashSet();
        var missing = wanted.Where(w => !have.Contains(w.Key)).ToList();
        if (missing.Count == 0)
        {
            return;
        }

        var slugs = missing.Select(m => m.Key).ToList();
        var existing = await db.Query<Platform>().Where(p => slugs.Contains(p.Slug)).ToListAsync(ct);
        foreach (var (slug, name) in missing)
        {
            var platform = existing.FirstOrDefault(p => p.Slug == slug);
            if (platform is null)
            {
                platform = new Platform { Name = name, Slug = slug };
                db.Set<Platform>().Add(platform);
            }

            game.GamePlatforms.Add(new GamePlatform { Platform = platform });
        }
    }

    /// <summary>The modes column is one delimited string; keep as many whole entries as fit.</summary>
    private static List<string> FitModes(IReadOnlyList<string> modes)
    {
        var result = new List<string>();
        var length = 0;
        foreach (var mode in modes.Where(m => !m.Contains('|')))
        {
            var next = length + mode.Length + (result.Count > 0 ? 1 : 0);
            if (next > MaxGameModesLength)
            {
                break;
            }

            result.Add(mode);
            length = next;
        }

        return result;
    }
}
