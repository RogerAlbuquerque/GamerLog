namespace GameLog.Domain.Entities;

/// <summary>
/// A comment on a game or on a review (exactly one of <see cref="GameId"/> /
/// <see cref="ReviewId"/> is set). Replies are one level deep: a reply to a reply
/// attaches to the same root comment.
/// </summary>
public class Comment
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid? GameId { get; set; }
    public Guid? ReviewId { get; set; }
    public Guid? ParentCommentId { get; set; }
    public string Body { get; set; } = "";
    public bool IsSpoiler { get; set; }
    public int LikeCount { get; set; }
    public int ReplyCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? EditedAt { get; set; }
    /// <summary>Soft delete keeps reply threads intact.</summary>
    public DateTime? DeletedAt { get; set; }
}

public class CommentLike
{
    public Guid CommentId { get; set; }
    public Comment Comment { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
