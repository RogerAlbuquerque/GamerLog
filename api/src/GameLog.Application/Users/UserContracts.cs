namespace GameLog.Application.Users;

/// <summary>Public profile data. Email and other private fields are never part of it.</summary>
public sealed record ProfileDto(
    Guid Id,
    string Username,
    string DisplayName,
    string? Bio,
    string? AvatarUrl,
    DateTime JoinedAt);

/// <summary>Null fields are left unchanged; an empty bio clears it.</summary>
public sealed record UpdateProfileRequest(string? DisplayName, string? Bio, bool? ShowSpoilersByDefault);

public sealed record AvatarResponse(string? AvatarUrl);
