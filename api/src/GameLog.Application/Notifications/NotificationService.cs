using GameLog.Application.Abstractions;
using GameLog.Application.Common;
using GameLog.Application.Reviews;
using GameLog.Domain.Entities;
using GameLog.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GameLog.Application.Notifications;

/// <summary>
/// In-app notifications (new follower, likes, replies). <see cref="Stage"/> only adds the row to the
/// change tracker so it is saved atomically with the action that caused it.
/// </summary>
public sealed class NotificationService(IAppDbContext db, TimeProvider clock)
{
    /// <summary>
    /// Queues a notification. Acting on your own content never notifies you, and likes are de-duplicated
    /// so like/unlike/like does not spam the recipient.
    /// </summary>
    public async Task StageAsync(
        Guid recipientId, Guid actorId, NotificationType type,
        Guid? gameId = null, Guid? reviewId = null, Guid? commentId = null, CancellationToken ct = default)
    {
        if (recipientId == actorId)
        {
            return;
        }

        var dedupe = type is NotificationType.NewFollower or NotificationType.ReviewLiked or NotificationType.CommentLiked;
        if (dedupe && await db.ReadOnly<Notification>().AnyAsync(n =>
                n.RecipientId == recipientId && n.ActorId == actorId && n.Type == type
                && n.ReviewId == reviewId && n.CommentId == commentId, ct))
        {
            return;
        }

        db.Set<Notification>().Add(new Notification
        {
            RecipientId = recipientId, ActorId = actorId, Type = type,
            GameId = gameId, ReviewId = reviewId, CommentId = commentId,
            CreatedAt = clock.GetUtcNow().UtcDateTime,
        });
    }

    public async Task<PagedResult<NotificationDto>> ListAsync(
        Guid userId, bool unreadOnly, int? page, int? pageSize, CancellationToken ct = default)
    {
        var q = db.ReadOnly<Notification>().Where(n => n.RecipientId == userId);
        if (unreadOnly)
        {
            q = q.Where(n => n.ReadAt == null);
        }

        var rows = await q.OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id)
            .Select(n => new
            {
                n.Id, n.Type, n.ActorId, n.Actor.Username, n.Actor.DisplayName, n.Actor.AvatarKey,
                n.GameId, n.ReviewId, n.CommentId, n.ReadAt, n.CreatedAt,
            })
            .ToPagedAsync(page, pageSize, ct);

        var gameIds = rows.Items.Where(r => r.GameId != null).Select(r => r.GameId!.Value).Distinct().ToList();
        var games = await db.ReadOnly<Game>().Where(g => gameIds.Contains(g.Id))
            .Select(g => new { g.Id, g.Slug, g.Title }).ToDictionaryAsync(g => g.Id, ct);

        var items = rows.Items.Select(r =>
        {
            var game = r.GameId is { } id && games.TryGetValue(id, out var g) ? g : null;
            return new NotificationDto(
                r.Id, r.Type, new ReviewAuthorDto(r.ActorId, r.Username, r.DisplayName, MediaUrls.For(r.AvatarKey)),
                r.GameId, game?.Slug, game?.Title, r.ReviewId, r.CommentId, r.ReadAt != null, r.CreatedAt);
        }).ToList();
        return new PagedResult<NotificationDto>(items, rows.Page, rows.PageSize, rows.Total);
    }

    public async Task<UnreadCountDto> UnreadCountAsync(Guid userId, CancellationToken ct = default) =>
        new(await db.ReadOnly<Notification>().CountAsync(n => n.RecipientId == userId && n.ReadAt == null, ct));

    /// <summary>Marks one notification as read. Other users' notifications look like they do not exist.</summary>
    public async Task MarkReadAsync(Guid userId, Guid notificationId, CancellationToken ct = default)
    {
        var n = await db.Query<Notification>().FirstOrDefaultAsync(x => x.Id == notificationId && x.RecipientId == userId, ct)
                ?? throw new NotFoundException("Notification not found.");
        if (n.ReadAt is null)
        {
            n.ReadAt = clock.GetUtcNow().UtcDateTime;
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task MarkAllReadAsync(Guid userId, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var unread = await db.Query<Notification>().Where(n => n.RecipientId == userId && n.ReadAt == null).ToListAsync(ct);
        foreach (var n in unread)
        {
            n.ReadAt = now;
        }

        await db.SaveChangesAsync(ct);
    }
}
