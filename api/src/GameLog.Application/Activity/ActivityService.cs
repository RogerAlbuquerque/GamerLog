using GameLog.Application.Abstractions;
using GameLog.Application.Common;
using GameLog.Domain.Entities;
using GameLog.Domain.Enums;
using GameLog.Domain.Rules;
using GameLog.Application.Reviews;
using Microsoft.EntityFrameworkCore;

namespace GameLog.Application.Activity;

/// <summary>
/// The social activity feed. Other services call <see cref="StageAsync"/> inside their own unit of work so an
/// activity item is saved atomically with the thing it describes. Repeating the same action shortly
/// afterwards (re-rating a game, say) refreshes the existing item instead of flooding followers.
/// </summary>
public sealed class ActivityService(IAppDbContext db, TimeProvider clock)
{
    public static readonly TimeSpan CollapseWindow = TimeSpan.FromHours(24);

    public async Task StageAsync(
        Guid actorId, ActivityType type, Guid? gameId = null, Guid? reviewId = null,
        Guid? collectionId = null, int? value = null, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var since = now - CollapseWindow;
        var existing = await db.Query<ActivityFeedItem>().FirstOrDefaultAsync(a =>
            a.ActorId == actorId && a.Type == type && a.GameId == gameId && a.ReviewId == reviewId
            && a.CollectionId == collectionId && a.CreatedAt >= since, ct);

        if (existing is not null)
        {
            existing.Value = value;
            existing.CreatedAt = now;
            return;
        }

        db.Set<ActivityFeedItem>().Add(new ActivityFeedItem
        {
            ActorId = actorId, Type = type, GameId = gameId, ReviewId = reviewId,
            CollectionId = collectionId, Value = value, CreatedAt = now,
        });
    }

    /// <summary>Withdraws activity about something that was removed (an un-rated game, a deleted review).</summary>
    public async Task WithdrawAsync(Guid actorId, ActivityType type, Guid? gameId = null, Guid? reviewId = null, CancellationToken ct = default)
    {
        var items = await db.Query<ActivityFeedItem>()
            .Where(a => a.ActorId == actorId && a.Type == type && a.GameId == gameId && a.ReviewId == reviewId)
            .ToListAsync(ct);
        db.Set<ActivityFeedItem>().RemoveRange(items);
    }

    /// <summary>What the people you follow (and you) have been doing, newest first.</summary>
    public Task<PagedResult<ActivityDto>> FeedAsync(Guid userId, int? page, int? pageSize, CancellationToken ct = default)
    {
        var followed = db.ReadOnly<UserFollow>().Where(f => f.FollowerId == userId).Select(f => f.FolloweeId);
        return PageAsync(db.ReadOnly<ActivityFeedItem>().Where(a => a.ActorId == userId || followed.Contains(a.ActorId)), page, pageSize, ct);
    }

    /// <summary>One user's public activity (their profile timeline).</summary>
    public async Task<PagedResult<ActivityDto>> ForUserAsync(string username, int? page, int? pageSize, CancellationToken ct = default)
    {
        var normalized = UserRules.NormalizeUsername(username);
        var id = await db.ReadOnly<User>().Where(u => u.UsernameNormalized == normalized).Select(u => (Guid?)u.Id).FirstOrDefaultAsync(ct)
                 ?? throw new NotFoundException("User not found.");
        return await PageAsync(db.ReadOnly<ActivityFeedItem>().Where(a => a.ActorId == id), page, pageSize, ct);
    }

    private async Task<PagedResult<ActivityDto>> PageAsync(IQueryable<ActivityFeedItem> items, int? page, int? pageSize, CancellationToken ct)
    {
        var rows = await items.OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id)
            .Select(a => new
            {
                a.Id, a.Type, a.ActorId, a.Actor.Username, a.Actor.DisplayName, a.Actor.AvatarKey,
                a.GameId, a.ReviewId, a.CollectionId, a.Value, a.CreatedAt,
            })
            .ToPagedAsync(page, pageSize, ct);

        var gameIds = rows.Items.Where(r => r.GameId != null).Select(r => r.GameId!.Value).Distinct().ToList();
        var reviewIds = rows.Items.Where(r => r.ReviewId != null).Select(r => r.ReviewId!.Value).Distinct().ToList();
        var collectionIds = rows.Items.Where(r => r.CollectionId != null).Select(r => r.CollectionId!.Value).Distinct().ToList();

        var games = await db.ReadOnly<Game>().Where(g => gameIds.Contains(g.Id))
            .Select(g => new { g.Id, g.Slug, g.Title, g.CoverKey }).ToDictionaryAsync(g => g.Id, ct);
        var reviews = await db.ReadOnly<Review>().Where(r => reviewIds.Contains(r.Id))
            .Select(r => new { r.Id, r.Title }).ToDictionaryAsync(r => r.Id, ct);
        var collections = await db.ReadOnly<GameCollection>().Where(c => collectionIds.Contains(c.Id) && c.Visibility == Visibility.Public)
            .Select(c => new { c.Id, c.Name }).ToDictionaryAsync(c => c.Id, ct);

        var result = new List<ActivityDto>();
        foreach (var r in rows.Items)
        {
            // Items about removed or private things are dropped from the page rather than rendered half-empty.
            if (r.ReviewId is { } rid && !reviews.ContainsKey(rid))
            {
                continue;
            }

            if (r.CollectionId is { } cid && !collections.ContainsKey(cid))
            {
                continue;
            }

            var game = r.GameId is { } gid && games.TryGetValue(gid, out var g) ? new ActivityGameDto(g.Id, g.Slug, g.Title, MediaUrls.For(g.CoverKey)) : null;
            result.Add(new ActivityDto(
                r.Id, r.Type, new ReviewAuthorDto(r.ActorId, r.Username, r.DisplayName, MediaUrls.For(r.AvatarKey)), game,
                r.ReviewId, r.ReviewId is { } rv ? reviews[rv].Title : null,
                r.CollectionId, r.CollectionId is { } cv ? collections[cv].Name : null,
                r.Type == ActivityType.GameRated ? r.Value : null, r.CreatedAt));
        }

        return new PagedResult<ActivityDto>(result, rows.Page, rows.PageSize, rows.Total);
    }
}
