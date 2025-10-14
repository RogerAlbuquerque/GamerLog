using GameLog.Application.Abstractions;
using GameLog.Application.Activity;
using GameLog.Application.Common;
using GameLog.Application.Reviews;
using GameLog.Domain.Entities;
using GameLog.Domain.Enums;
using GameLog.Domain.Rules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameLog.Application.Collections;

/// <summary>
/// Ordered, optionally private lists of games. Private collections are invisible (404, not 403) to everyone
/// except their owner, and every write verifies ownership against the authenticated user.
/// </summary>
public sealed class CollectionService(
    IAppDbContext db,
    IObjectStorage storage,
    IImageProcessor images,
    ActivityService activity,
    TimeProvider clock,
    ILogger<CollectionService> logger)
{
    private sealed record Row(
        Guid Id, Guid OwnerId, string Username, string DisplayName, string? AvatarKey, string Name, string? Description,
        string? CoverKey, Visibility Visibility, int GameCount, DateTime CreatedAt, DateTime UpdatedAt);

    private IQueryable<Row> Project(IQueryable<GameCollection> q) => q.Select(c => new Row(
        c.Id, c.UserId, c.User.Username, c.User.DisplayName, c.User.AvatarKey, c.Name, c.Description, c.CoverKey,
        c.Visibility, c.Games.Count, c.CreatedAt, c.UpdatedAt));

    private async Task<List<CollectionSummaryDto>> ToSummariesAsync(List<Row> rows, CancellationToken ct)
    {
        var ids = rows.Select(r => r.Id).ToList();
        var covers = await db.ReadOnly<CollectionGame>()
            .Where(cg => ids.Contains(cg.CollectionId) && cg.Game.CoverKey != null)
            .OrderBy(cg => cg.Position)
            .Select(cg => new { cg.CollectionId, cg.Game.CoverKey })
            .ToListAsync(ct);
        var byCollection = covers.GroupBy(c => c.CollectionId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Take(4).Select(c => MediaUrls.For(c.CoverKey)!).ToList());

        return rows.Select(r =>
        {
            var previews = byCollection.GetValueOrDefault(r.Id) ?? [];
            return new CollectionSummaryDto(
                r.Id, new ReviewAuthorDto(r.OwnerId, r.Username, r.DisplayName, MediaUrls.For(r.AvatarKey)), r.Name, r.Description,
                MediaUrls.For(r.CoverKey) ?? previews.FirstOrDefault(), r.Visibility, r.GameCount, previews, r.CreatedAt, r.UpdatedAt);
        }).ToList();
    }

    private static IQueryable<GameCollection> VisibleTo(IQueryable<GameCollection> q, Guid? viewerId) =>
        q.Where(c => c.Visibility == Visibility.Public || c.UserId == viewerId);

    public async Task<PagedResult<CollectionSummaryDto>> ListForUserAsync(
        string username, Guid? viewerId, int? page, int? pageSize, CancellationToken ct = default)
    {
        var normalized = UserRules.NormalizeUsername(username);
        var ownerId = await db.ReadOnly<User>().Where(u => u.UsernameNormalized == normalized).Select(u => (Guid?)u.Id).FirstOrDefaultAsync(ct)
                      ?? throw new NotFoundException("User not found.");
        var q = VisibleTo(db.ReadOnly<GameCollection>().Where(c => c.UserId == ownerId), viewerId)
            .OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id);
        var rows = await Project(q).ToPagedAsync(page, pageSize, ct);
        return new PagedResult<CollectionSummaryDto>(await ToSummariesAsync(rows.Items.ToList(), ct), rows.Page, rows.PageSize, rows.Total);
    }

    /// <summary>Public collections of everyone, newest first, for discovery.</summary>
    public async Task<PagedResult<CollectionSummaryDto>> ListPublicAsync(int? page, int? pageSize, CancellationToken ct = default)
    {
        var q = db.ReadOnly<GameCollection>().Where(c => c.Visibility == Visibility.Public && c.Games.Any())
            .OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id);
        var rows = await Project(q).ToPagedAsync(page, pageSize, ct);
        return new PagedResult<CollectionSummaryDto>(await ToSummariesAsync(rows.Items.ToList(), ct), rows.Page, rows.PageSize, rows.Total);
    }

    public async Task<CollectionDetailDto> GetAsync(Guid id, Guid? viewerId, CancellationToken ct = default)
    {
        var row = await Project(VisibleTo(db.ReadOnly<GameCollection>().Where(c => c.Id == id), viewerId)).FirstOrDefaultAsync(ct)
                  ?? throw new NotFoundException("Collection not found.");
        var summary = (await ToSummariesAsync([row], ct))[0];
        var games = await db.ReadOnly<CollectionGame>().Where(cg => cg.CollectionId == id).OrderBy(cg => cg.Position)
            .Select(cg => new { cg.GameId, cg.Game.Slug, cg.Game.Title, cg.Game.CoverKey, cg.Game.ReleaseDate, cg.Position })
            .ToListAsync(ct);
        return new CollectionDetailDto(summary, games.Select(g =>
            new CollectionGameDto(g.GameId, g.Slug, g.Title, MediaUrls.For(g.CoverKey), g.ReleaseDate?.Year, g.Position)).ToList());
    }

    public async Task<CollectionDetailDto> CreateAsync(Guid userId, SaveCollectionRequest req, CancellationToken ct = default)
    {
        Validate(req);
        var now = clock.GetUtcNow().UtcDateTime;
        var collection = new GameCollection
        {
            UserId = userId, Name = req.Name.Trim(), Description = ReviewRules.Clean(req.Description),
            Visibility = req.Visibility, CreatedAt = now, UpdatedAt = now,
        };
        db.Set<GameCollection>().Add(collection);
        if (req.Visibility == Visibility.Public)
        {
            await activity.StageAsync(userId, ActivityType.CollectionCreated, collectionId: collection.Id, ct: ct);
        }

        await db.SaveChangesAsync(ct);
        return await GetAsync(collection.Id, userId, ct);
    }

    public async Task<CollectionDetailDto> UpdateAsync(Guid userId, Guid id, SaveCollectionRequest req, CancellationToken ct = default)
    {
        Validate(req);
        var collection = await LoadOwnedAsync(userId, id, ct);
        collection.Name = req.Name.Trim();
        collection.Description = ReviewRules.Clean(req.Description);
        collection.Visibility = req.Visibility;
        collection.UpdatedAt = clock.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, userId, ct);
    }

    public async Task DeleteAsync(Guid userId, bool isAdmin, Guid id, CancellationToken ct = default)
    {
        var collection = await db.Query<GameCollection>().FirstOrDefaultAsync(c => c.Id == id, ct)
                         ?? throw new NotFoundException("Collection not found.");
        if (collection.UserId != userId && !isAdmin)
        {
            // Private collections must not reveal their existence to other users.
            throw collection.Visibility == Visibility.Private ? new NotFoundException("Collection not found.") : new ForbiddenException();
        }

        var coverKey = collection.CoverKey;
        db.Set<GameCollection>().Remove(collection);
        await db.SaveChangesAsync(ct);
        await TryDeleteAsync(coverKey);
    }

    public async Task<CollectionDetailDto> AddGameAsync(Guid userId, Guid id, Guid gameId, CancellationToken ct = default)
    {
        var collection = await LoadOwnedAsync(userId, id, ct);
        if (!await db.ReadOnly<Game>().AnyAsync(g => g.Id == gameId, ct))
        {
            throw new NotFoundException("Game not found.");
        }

        var positions = await db.ReadOnly<CollectionGame>().Where(cg => cg.CollectionId == id)
            .Select(cg => new { cg.GameId, cg.Position }).ToListAsync(ct);
        if (positions.Any(p => p.GameId == gameId))
        {
            throw new ConflictException("already_in_collection", "This game is already in the collection.");
        }

        if (positions.Count >= CollectionRules.MaxGames)
        {
            throw new ValidationFailedException("gameId", $"A collection can hold at most {CollectionRules.MaxGames} games.");
        }

        var now = clock.GetUtcNow().UtcDateTime;
        db.Set<CollectionGame>().Add(new CollectionGame
        {
            CollectionId = id, GameId = gameId, AddedAt = now,
            Position = positions.Count == 0 ? 0 : positions.Max(p => p.Position) + 1,
        });
        collection.UpdatedAt = now;
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            throw new ConflictException("collection_conflict", "The collection was changed at the same time. Please try again.");
        }

        return await GetAsync(id, userId, ct);
    }

    public async Task<CollectionDetailDto> RemoveGameAsync(Guid userId, Guid id, Guid gameId, CancellationToken ct = default)
    {
        var collection = await LoadOwnedAsync(userId, id, ct);
        var item = await db.Query<CollectionGame>().FirstOrDefaultAsync(cg => cg.CollectionId == id && cg.GameId == gameId, ct)
                   ?? throw new NotFoundException("That game is not in the collection.");
        db.Set<CollectionGame>().Remove(item);
        collection.UpdatedAt = clock.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, userId, ct);
    }

    /// <summary>
    /// Applies a complete new order. (CollectionId, Position) is unique and PostgreSQL checks it row by row, so the
    /// positions are first moved out of the way and then set to their final values, inside one transaction.
    /// </summary>
    public async Task<CollectionDetailDto> ReorderAsync(Guid userId, Guid id, IReadOnlyList<Guid> orderedGameIds, CancellationToken ct = default)
    {
        var collection = await LoadOwnedAsync(userId, id, ct);
        var items = await db.Query<CollectionGame>().Where(cg => cg.CollectionId == id).ToListAsync(ct);
        if (!CollectionRules.IsPermutation(items.Select(i => i.GameId).ToList(), orderedGameIds ?? []))
        {
            throw new ValidationFailedException("gameIds", "Send every game in the collection exactly once.");
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        const int offset = 1_000_000;
        foreach (var item in items)
        {
            item.Position += offset;
        }

        await db.SaveChangesAsync(ct);
        for (var i = 0; i < orderedGameIds!.Count; i++)
        {
            items.Single(x => x.GameId == orderedGameIds[i]).Position = i;
        }

        collection.UpdatedAt = clock.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return await GetAsync(id, userId, ct);
    }

    public async Task<CollectionCoverResponse> SetCoverAsync(Guid userId, Guid id, Stream upload, CancellationToken ct = default)
    {
        var collection = await LoadOwnedAsync(userId, id, ct);
        var processed = await images.ProcessAsync(upload, ImageKind.Cover, ct);
        var newKey = ObjectKeys.New("collections", processed.Extension);
        await storage.PutAsync(newKey, new MemoryStream(processed.Data), processed.ContentType, ct);

        var oldKey = collection.CoverKey;
        collection.CoverKey = newKey;
        collection.UpdatedAt = clock.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);
        await TryDeleteAsync(oldKey);
        return new CollectionCoverResponse(MediaUrls.For(newKey));
    }

    public async Task RemoveCoverAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var collection = await LoadOwnedAsync(userId, id, ct);
        var oldKey = collection.CoverKey;
        collection.CoverKey = null;
        await db.SaveChangesAsync(ct);
        await TryDeleteAsync(oldKey);
    }

    private static void Validate(SaveCollectionRequest req)
    {
        var errors = CollectionRules.Validate(req.Name, req.Description);
        if (errors.Count > 0)
        {
            throw ValidationFailedException.From(errors);
        }
    }

    /// <summary>Loads a collection for writing. Anyone but the owner sees "not found", so ids reveal nothing.</summary>
    private async Task<GameCollection> LoadOwnedAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var collection = await db.Query<GameCollection>().FirstOrDefaultAsync(c => c.Id == id, ct)
                         ?? throw new NotFoundException("Collection not found.");
        if (collection.UserId != userId)
        {
            throw collection.Visibility == Visibility.Private
                ? new NotFoundException("Collection not found.")
                : new ForbiddenException("Only the owner can change this collection.");
        }

        return collection;
    }

    private async Task TryDeleteAsync(string? key)
    {
        if (key is null)
        {
            return;
        }

        try
        {
            await storage.DeleteAsync(key);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not delete stored object {Key}", key);
        }
    }
}
