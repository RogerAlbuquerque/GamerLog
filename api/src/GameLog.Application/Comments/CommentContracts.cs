using GameLog.Application.Reviews;

namespace GameLog.Application.Comments;

public sealed record CommentDto(
    Guid Id,
    Guid? ParentId,
    ReviewAuthorDto? Author,
    string Body,
    bool IsSpoiler,
    bool IsDeleted,
    int LikeCount,
    int ReplyCount,
    bool LikedByMe,
    DateTime CreatedAt,
    DateTime? EditedAt);

public sealed record CreateCommentRequest(string Body, bool IsSpoiler, Guid? ParentCommentId);

public sealed record EditCommentRequest(string Body, bool IsSpoiler);

public static class CommentSort
{
    public const string Newest = "newest";
    public const string Top = "top";
}
