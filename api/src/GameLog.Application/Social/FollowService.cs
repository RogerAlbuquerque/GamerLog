using GameLog.Application.Abstractions;
using GameLog.Application.Common;
using GameLog.Application.Notifications;
using GameLog.Domain.Entities;
using GameLog.Domain.Enums;
using GameLog.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace GameLog.Application.Social;

public sealed class FollowService(IAppDbContext db, NotificationService notifications, TimeProvider clock)
{
    private async Task<Guid> ResolveAsync(string username, CancellationToken ct)
    {
        var normalized = UserRules.NormalizeUsername(username);
        return await db.ReadOnly<User>().Where(u => u.UsernameNormalized == normalized).Select(u => (Guid?)u.Id).FirstOrDefaultAsync(ct)
               ?? throw new NotFoundException("User not found.");
    }

    public async Task<FollowStateDto> StateAsync(string username, Guid? viewerId, CancellationToken ct = default)
    {
        var id = await ResolveAsync(username, ct);
        var followers = await db.ReadOnly<UserFollow>().CountAsync(f => f.FolloweeId == id, ct);
        var following = await db.ReadOnly<UserFollow>().CountAsync(f => f.FollowerId == id, ct);
        var isFollowing = viewerId is { } v && await db.ReadOnly<UserFollow>().AnyAsync(f => f.FollowerId == v && f.FolloweeId == id, ct);
        return new FollowStateDto(isFollowing, followers, following);
    }

    /// <summary>Idempotent: following someone twice is the same as following once.</summary>
    public async Task<FollowStateDto> FollowAsync(Guid followerId, string username, CancellationToken ct = default)
    {
        var targetId = await ResolveAsync(username, ct);
        if (targetId == followerId)
        {
            throw new ValidationFailedException("username", "You cannot follow yourself.");
        }

        if (!await db.ReadOnly<UserFollow>().AnyAsync(f => f.FollowerId == followerId && f.FolloweeId == targetId, ct))
        {
            db.Set<UserFollow>().Add(new UserFollow { FollowerId = followerId, FolloweeId = targetId, CreatedAt = clock.GetUtcNow().UtcDateTime });
            await notifications.StageAsync(targetId, followerId, NotificationType.NewFollower, ct: ct);
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // A concurrent identical request won; the end state is the same.
            }
        }

        return await StateAsync(username, followerId, ct);
    }

    public async Task<FollowStateDto> UnfollowAsync(Guid followerId, string username, CancellationToken ct = default)
    {
        var targetId = await ResolveAsync(username, ct);
        var follow = await db.Query<UserFollow>().FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FolloweeId == targetId, ct);
        if (follow is not null)
        {
            db.Set<UserFollow>().Remove(follow);
            await db.SaveChangesAsync(ct);
        }

        return await StateAsync(username, followerId, ct);
    }

    public async Task<PagedResult<UserListItemDto>> FollowersAsync(string username, Guid? viewerId, int? page, int? pageSize, CancellationToken ct = default)
    {
        var id = await ResolveAsync(username, ct);
        var q = db.ReadOnly<UserFollow>().Where(f => f.FolloweeId == id).OrderByDescending(f => f.CreatedAt).Select(f => f.Follower);
        return await ListAsync(q, viewerId, page, pageSize, ct);
    }

    public async Task<PagedResult<UserListItemDto>> FollowingAsync(string username, Guid? viewerId, int? page, int? pageSize, CancellationToken ct = default)
    {
        var id = await ResolveAsync(username, ct);
        var q = db.ReadOnly<UserFollow>().Where(f => f.FollowerId == id).OrderByDescending(f => f.CreatedAt).Select(f => f.Followee);
        return await ListAsync(q, viewerId, page, pageSize, ct);
    }

    private async Task<PagedResult<UserListItemDto>> ListAsync(
        IQueryable<User> users, Guid? viewerId, int? page, int? pageSize, CancellationToken ct)
    {
        var follows = db.ReadOnly<UserFollow>();
        var result = await users.Select(u => new
        {
            u.Id, u.Username, u.DisplayName, u.AvatarKey,
            Followed = viewerId != null && follows.Any(f => f.FollowerId == viewerId && f.FolloweeId == u.Id),
        }).ToPagedAsync(page, pageSize, ct);
        return new PagedResult<UserListItemDto>(
            result.Items.Select(u => new UserListItemDto(u.Id, u.Username, u.DisplayName, MediaUrls.For(u.AvatarKey), u.Followed)).ToList(),
            result.Page, result.PageSize, result.Total);
    }
}
