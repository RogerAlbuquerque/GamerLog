using GameLog.Domain.Enums;

namespace GameLog.Domain.Entities;

public class UserFollow
{
    public Guid FollowerId { get; set; }
    public User Follower { get; set; } = null!;
    public Guid FolloweeId { get; set; }
    public User Followee { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

/// <summary>Something a user did, shown to their followers. Table: ActivityFeed.</summary>
public class ActivityFeedItem
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid ActorId { get; set; }
    public User Actor { get; set; } = null!;
    public ActivityType Type { get; set; }
    public Guid? GameId { get; set; }
    public Game? Game { get; set; }
    public Guid? ReviewId { get; set; }
    public Guid? CollectionId { get; set; }
    /// <summary>Rating in half-stars for <see cref="ActivityType.GameRated"/>.</summary>
    public int? Value { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class Notification
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid RecipientId { get; set; }
    public User Recipient { get; set; } = null!;
    public Guid ActorId { get; set; }
    public User Actor { get; set; } = null!;
    public NotificationType Type { get; set; }
    public Guid? GameId { get; set; }
    public Guid? ReviewId { get; set; }
    public Guid? CommentId { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
