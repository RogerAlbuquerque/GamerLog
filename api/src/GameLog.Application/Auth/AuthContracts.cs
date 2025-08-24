using GameLog.Application.Common;
using GameLog.Domain.Entities;
using GameLog.Domain.Enums;

namespace GameLog.Application.Auth;

public sealed record RegisterRequest(string Username, string Email, string Password, string? DisplayName);

/// <param name="Login">Username or email address.</param>
public sealed record LoginRequest(string Login, string Password);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record DeleteAccountRequest(string Password);

/// <summary>What the client may know about the signed-in user.</summary>
public sealed record SessionUser(
    Guid Id,
    string Username,
    string DisplayName,
    string? AvatarUrl,
    UserRole Role,
    bool ShowSpoilersByDefault)
{
    public static SessionUser From(User u) =>
        new(u.Id, u.Username, u.DisplayName, MediaUrls.For(u.AvatarKey), u.Role, u.ShowSpoilersByDefault);
}

public sealed class AccountLockedException()
    : AppException(429, "account_locked", "Too many failed sign-in attempts. Try again in a few minutes.");
