using GameLog.Domain.Enums;

namespace GameLog.Domain.Entities;

/// <summary>A user's independent star rating of a game, in half-stars (1-10).</summary>
public class Rating
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid GameId { get; set; }
    public Game Game { get; set; } = null!;
    public int HalfStars { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>The user's primary review of a game. The score lives in <see cref="Rating"/>.</summary>
public class Review
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid GameId { get; set; }
    public Game Game { get; set; } = null!;
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string? Experience { get; set; }
    public string? Pros { get; set; }
    public string? Cons { get; set; }
    public string? FavoriteCharacter { get; set; }
    public string? FavoriteMoment { get; set; }
    public Difficulty? Difficulty { get; set; }
    public int? PlaytimeMinutes { get; set; }
    public int? PlatformId { get; set; }
    public Platform? Platform { get; set; }
    public DateOnly? CompletedOn { get; set; }
    public bool ContainsSpoilers { get; set; }
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ReviewLike
{
    public Guid ReviewId { get; set; }
    public Review Review { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
