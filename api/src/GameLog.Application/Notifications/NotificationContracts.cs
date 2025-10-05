using GameLog.Application.Reviews;
using GameLog.Domain.Enums;

namespace GameLog.Application.Notifications;

public sealed record NotificationDto(
    Guid Id,
    NotificationType Type,
    ReviewAuthorDto Actor,
    Guid? GameId,
    string? GameSlug,
    string? GameTitle,
    Guid? ReviewId,
    Guid? CommentId,
    bool IsRead,
    DateTime CreatedAt);

public sealed record UnreadCountDto(int Count);
