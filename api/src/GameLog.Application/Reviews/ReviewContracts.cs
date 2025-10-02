using GameLog.Domain.Enums;

namespace GameLog.Application.Reviews;

public sealed record ReviewAuthorDto(Guid Id, string Username, string DisplayName, string? AvatarUrl);

public sealed record ReviewDto(
    Guid Id,
    Guid GameId,
    string GameSlug,
    string GameTitle,
    string? CoverUrl,
    ReviewAuthorDto Author,
    string Title,
    string Body,
    string? Experience,
    string? Pros,
    string? Cons,
    string? FavoriteCharacter,
    string? FavoriteMoment,
    Difficulty? Difficulty,
    int? PlaytimeMinutes,
    int? PlatformId,
    string? Platform,
    DateOnly? CompletedOn,
    bool ContainsSpoilers,
    int? RatingHalfStars,
    int LikeCount,
    int CommentCount,
    bool LikedByMe,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>
/// Creates or replaces the caller's review of a game. When <see cref="HalfStars"/> is given the game
/// rating is set in the same transaction; when omitted the existing rating is left alone.
/// </summary>
public sealed record SaveReviewRequest(
    string Title,
    string Body,
    bool ContainsSpoilers,
    int? HalfStars,
    string? Experience,
    string? Pros,
    string? Cons,
    string? FavoriteCharacter,
    string? FavoriteMoment,
    Difficulty? Difficulty,
    int? PlaytimeMinutes,
    int? PlatformId,
    DateOnly? CompletedOn);

public sealed record LikeResponse(bool Liked, int LikeCount);

public static class ReviewSort
{
    public const string Newest = "newest";
    public const string Popular = "popular";
}
