using GameLog.Application.Reviews;

namespace GameLog.Application.Social;

public sealed record FollowStateDto(bool Following, int Followers, int FollowingCount);

public sealed record UserListItemDto(Guid Id, string Username, string DisplayName, string? AvatarUrl, bool IsFollowedByMe);
