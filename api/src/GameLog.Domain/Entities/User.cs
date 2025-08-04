using GameLog.Domain.Enums;

namespace GameLog.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Username { get; set; } = "";
    /// <summary>Lower-case username used for the unique index.</summary>
    public string UsernameNormalized { get; set; } = "";
    public string Email { get; set; } = "";
    public string EmailNormalized { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? Bio { get; set; }
    public string? AvatarKey { get; set; }
    public bool ShowSpoilersByDefault { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    /// <summary>Rotated on password change; sessions carrying an old stamp are rejected.</summary>
    public Guid SecurityStamp { get; set; } = Guid.NewGuid();
    public int FailedLoginCount { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public DateTime CreatedAt { get; set; }
}
