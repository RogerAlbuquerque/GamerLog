using GameLog.Application.Abstractions;
using GameLog.Application.Common;
using GameLog.Application.Notifications;
using GameLog.Application.Reviews;
using GameLog.Domain.Entities;
using GameLog.Domain.Enums;
using GameLog.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace GameLog.Application.Comments;

/// <summary>What a comment thread hangs off: exactly one of a game or a review.</summary>
public readonly record struct CommentTarget(Guid? GameId, Guid? ReviewId)
{
    public static CommentTarget Game(Guid id) => new(id, null);

    public static CommentTarget Review(Guid id) => new(null, id);
}

/// <summary>
/// Threaded comments on games and reviews. Replies are one level deep (a reply to a reply attaches to
/// the root). Deleting is soft, so a root with replies stays as a "[deleted]" placeholder.
/// </summary>
public sealed class CommentService(IAppDbContext db, NotificationService notifications, TimeProvider clock)
{
    private IQueryable<Comment> ForTarget(CommentTarget t) =>
        t.GameId is { } g
            ? db.ReadOnly<Comment>().Where(c => c.GameId == g)
            : db.ReadOnly<Comment>().Where(c => c.ReviewId == t.ReviewId);

    private async Task EnsureTargetExistsAsync(CommentTarget t, CancellationToken ct)
    {
        var exists = t.GameId is { } g
            ? await db.ReadOnly<Game>().AnyAsync(x => x.Id == g, ct)
            : await db.ReadOnly<Review>().AnyAsync(x => x.Id == t.ReviewId, ct);
        if (!exists)
        {
            throw new NotFoundException(t.GameId is null ? "Review not found." : "Game not found.");
        }
    }

    private IQueryable<CommentRow> Project(IQueryable<Comment> comments, Guid? viewerId)
    {
        var likes = db.ReadOnly<CommentLike>();
        return comments.Select(c => new CommentRow(
            c.Id, c.ParentCommentId, c.UserId, c.User.Username, c.User.DisplayName, c.User.AvatarKey, c.Body, c.IsSpoiler,
            c.DeletedAt != null, c.LikeCount, c.ReplyCount,
            viewerId != null && likes.Any(l => l.CommentId == c.Id && l.UserId == viewerId),
            c.CreatedAt, c.EditedAt));
    }

    private sealed record CommentRow(
        Guid Id, Guid? ParentId, Guid UserId, string Username, string DisplayName, string? AvatarKey, string Body, bool IsSpoiler,
        bool IsDeleted, int LikeCount, int ReplyCount, bool LikedByMe, DateTime CreatedAt, DateTime? EditedAt);

    /// <summary>Deleted comments never leak their author or text.</summary>
    private static CommentDto ToDto(CommentRow r) => r.IsDeleted
        ? new CommentDto(r.Id, r.ParentId, null, "", false, true, 0, r.ReplyCount, false, r.CreatedAt, null)
        : new CommentDto(r.Id, r.ParentId, new ReviewAuthorDto(r.UserId, r.Username, r.DisplayName, MediaUrls.For(r.AvatarKey)),
            r.Body, r.IsSpoiler, false, r.LikeCount, r.ReplyCount, r.LikedByMe, r.CreatedAt, r.EditedAt);

    public async Task<PagedResult<CommentDto>> ListAsync(
        CommentTarget target, string? sort, Guid? viewerId, int? page, int? pageSize, CancellationToken ct = default)
    {
        await EnsureTargetExistsAsync(target, ct);
        var roots = ForTarget(target).Where(c => c.ParentCommentId == null && (c.DeletedAt == null || c.ReplyCount > 0));
        roots = sort == CommentSort.Top
            ? roots.OrderByDescending(c => c.LikeCount).ThenByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id)
            : roots.OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id);

        var result = await Project(roots, viewerId).ToPagedAsync(page, pageSize, ct);
        return new PagedResult<CommentDto>(result.Items.Select(ToDto).ToList(), result.Page, result.PageSize, result.Total);
    }

    /// <summary>Replies to a root comment, oldest first so conversations read top to bottom.</summary>
    public async Task<PagedResult<CommentDto>> RepliesAsync(
        Guid commentId, Guid? viewerId, int? page, int? pageSize, CancellationToken ct = default)
    {
        if (!await db.ReadOnly<Comment>().AnyAsync(c => c.Id == commentId, ct))
        {
            throw new NotFoundException("Comment not found.");
        }

        var replies = db.ReadOnly<Comment>()
            .Where(c => c.ParentCommentId == commentId && c.DeletedAt == null)
            .OrderBy(c => c.CreatedAt).ThenBy(c => c.Id);
        var result = await Project(replies, viewerId).ToPagedAsync(page, pageSize, ct);
        return new PagedResult<CommentDto>(result.Items.Select(ToDto).ToList(), result.Page, result.PageSize, result.Total);
    }

    public async Task<CommentDto> CreateAsync(
        Guid userId, CommentTarget target, CreateCommentRequest req, CancellationToken ct = default)
    {
        var (body, error) = CommentRules.Normalize(req.Body);
        if (error is not null)
        {
            throw new ValidationFailedException("body", error);
        }

        await EnsureTargetExistsAsync(target, ct);

        Guid? rootId = null;
        Guid? repliedToUserId = null;
        if (req.ParentCommentId is { } parentId)
        {
            var parent = await db.ReadOnly<Comment>().Where(c => c.Id == parentId)
                             .Select(c => new { c.Id, c.ParentCommentId, c.UserId, c.GameId, c.ReviewId, Deleted = c.DeletedAt != null })
                             .FirstOrDefaultAsync(ct)
                         ?? throw new NotFoundException("The comment you are replying to was not found.");
            if (parent.Deleted || parent.GameId != target.GameId || parent.ReviewId != target.ReviewId)
            {
                throw new ValidationFailedException("parentCommentId", "You cannot reply to this comment.");
            }

            rootId = parent.ParentCommentId ?? parent.Id;   // replies to replies attach to the root
            repliedToUserId = parent.UserId;
        }

        var comment = new Comment
        {
            UserId = userId, GameId = target.GameId, ReviewId = target.ReviewId, ParentCommentId = rootId,
            Body = body!, IsSpoiler = req.IsSpoiler, CreatedAt = clock.GetUtcNow().UtcDateTime,
        };
        db.Set<Comment>().Add(comment);

        if (repliedToUserId is { } repliedTo)
        {
            await notifications.StageAsync(repliedTo, userId, NotificationType.CommentReply, target.GameId, target.ReviewId, comment.Id, ct);
        }

        if (target.ReviewId is { } reviewId)
        {
            var reviewer = await db.ReadOnly<Review>().Where(r => r.Id == reviewId).Select(r => new { r.UserId, r.GameId }).FirstAsync(ct);
            // A reply notifies the person replied to; the review author is told about every other comment on their review.
            if (reviewer.UserId != repliedToUserId)
            {
                await notifications.StageAsync(reviewer.UserId, userId, NotificationType.ReviewComment, reviewer.GameId, reviewId, comment.Id, ct);
            }
        }

        await db.SaveChangesAsync(ct);
        await RecountAsync(comment, ct);
        return await GetAsync(comment.Id, userId, ct);
    }

    public async Task<CommentDto> GetAsync(Guid commentId, Guid? viewerId, CancellationToken ct = default)
    {
        var row = await Project(db.ReadOnly<Comment>().Where(c => c.Id == commentId), viewerId).FirstOrDefaultAsync(ct)
                  ?? throw new NotFoundException("Comment not found.");
        return ToDto(row);
    }

    public async Task<CommentDto> EditAsync(Guid userId, Guid commentId, EditCommentRequest req, CancellationToken ct = default)
    {
        var (body, error) = CommentRules.Normalize(req.Body);
        if (error is not null)
        {
            throw new ValidationFailedException("body", error);
        }

        var comment = await db.Query<Comment>().FirstOrDefaultAsync(c => c.Id == commentId && c.DeletedAt == null, ct)
                      ?? throw new NotFoundException("Comment not found.");
        if (comment.UserId != userId)
        {
            throw new ForbiddenException("You can only edit your own comments.");
        }

        comment.Body = body!;
        comment.IsSpoiler = req.IsSpoiler;
        comment.EditedAt = clock.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);
        return await GetAsync(commentId, userId, ct);
    }

    public async Task DeleteAsync(Guid actorId, bool isAdmin, Guid commentId, CancellationToken ct = default)
    {
        var comment = await db.Query<Comment>().FirstOrDefaultAsync(c => c.Id == commentId && c.DeletedAt == null, ct)
                      ?? throw new NotFoundException("Comment not found.");
        if (comment.UserId != actorId && !isAdmin)
        {
            throw new ForbiddenException("You can only delete your own comments.");
        }

        comment.DeletedAt = clock.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);
        await RecountAsync(comment, ct);
    }

    public async Task<LikeResponse> LikeAsync(Guid userId, Guid commentId, CancellationToken ct = default)
    {
        var target = await db.ReadOnly<Comment>().Where(c => c.Id == commentId && c.DeletedAt == null)
                         .Select(c => new { c.UserId, c.GameId, c.ReviewId }).FirstOrDefaultAsync(ct)
                     ?? throw new NotFoundException("Comment not found.");

        if (!await db.ReadOnly<CommentLike>().AnyAsync(l => l.CommentId == commentId && l.UserId == userId, ct))
        {
            db.Set<CommentLike>().Add(new CommentLike { CommentId = commentId, UserId = userId, CreatedAt = clock.GetUtcNow().UtcDateTime });
            await notifications.StageAsync(target.UserId, userId, NotificationType.CommentLiked, target.GameId, target.ReviewId, commentId, ct);
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // Concurrent duplicate like: same end state.
            }
        }

        return await RecountLikesAsync(commentId, true, ct);
    }

    public async Task<LikeResponse> UnlikeAsync(Guid userId, Guid commentId, CancellationToken ct = default)
    {
        if (!await db.ReadOnly<Comment>().AnyAsync(c => c.Id == commentId, ct))
        {
            throw new NotFoundException("Comment not found.");
        }

        var like = await db.Query<CommentLike>().FirstOrDefaultAsync(l => l.CommentId == commentId && l.UserId == userId, ct);
        if (like is not null)
        {
            db.Set<CommentLike>().Remove(like);
            await db.SaveChangesAsync(ct);
        }

        return await RecountLikesAsync(commentId, false, ct);
    }

    private async Task<LikeResponse> RecountLikesAsync(Guid commentId, bool liked, CancellationToken ct)
    {
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""UPDATE "Comments" SET "LikeCount" = (SELECT COUNT(*) FROM "CommentLikes" WHERE "CommentId" = {commentId}) WHERE "Id" = {commentId}""", ct);
        var count = await db.ReadOnly<Comment>().Where(c => c.Id == commentId).Select(c => c.LikeCount).FirstAsync(ct);
        return new LikeResponse(liked, count);
    }

    /// <summary>Refreshes the reply counter of the root and the comment counter of the review, from source rows.</summary>
    private async Task RecountAsync(Comment c, CancellationToken ct)
    {
        if (c.ParentCommentId is { } root)
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""UPDATE "Comments" SET "ReplyCount" = (SELECT COUNT(*) FROM "Comments" r WHERE r."ParentCommentId" = {root} AND r."DeletedAt" IS NULL) WHERE "Id" = {root}""", ct);
        }

        if (c.ReviewId is { } review)
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""UPDATE "Reviews" SET "CommentCount" = (SELECT COUNT(*) FROM "Comments" WHERE "ReviewId" = {review} AND "DeletedAt" IS NULL) WHERE "Id" = {review}""", ct);
        }
    }
}
