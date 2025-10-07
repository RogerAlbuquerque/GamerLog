using GameLog.Application.Reviews;
using GameLog.Domain.Enums;

namespace GameLog.Application.Activity;

public sealed record ActivityGameDto(Guid Id, string Slug, string Title, string? CoverUrl);

public sealed record ActivityDto(
    Guid Id,
    ActivityType Type,
    ReviewAuthorDto Actor,
    ActivityGameDto? Game,
    Guid? ReviewId,
    string? ReviewTitle,
    Guid? CollectionId,
    string? CollectionName,
    int? RatingHalfStars,
    DateTime CreatedAt);
