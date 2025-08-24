using GameLog.Application.Abstractions;
using GameLog.Application.Common;
using GameLog.Domain.Entities;
using GameLog.Domain.Rules;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GameLog.Application.Auth;

public sealed class AuthService(IAppDbContext db, IPasswordHasher<User> hasher, TimeProvider clock)
{
    public async Task<User> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var errors = new Dictionary<string, string[]>();
        if (UserRules.ValidateUsername(request.Username) is { } usernameError) errors["username"] = [usernameError];
        if (UserRules.ValidateEmail(request.Email) is { } emailError) errors["email"] = [emailError];
        if (UserRules.ValidatePassword(request.Password, request.Username?.Trim()) is { } passwordError) errors["password"] = [passwordError];

        var displayName = string.IsNullOrWhiteSpace(request.DisplayName) ? request.Username?.Trim() : request.DisplayName.Trim();
        if (displayName is { Length: > 50 }) errors["displayName"] = ["Display name must be at most 50 characters."];
        if (errors.Count > 0) throw new ValidationFailedException(errors);

        var username = request.Username.Trim();
        var usernameNormalized = UserRules.NormalizeUsername(username);
        var emailNormalized = UserRules.NormalizeEmail(request.Email);

        if (await db.Set<User>().AnyAsync(u => u.UsernameNormalized == usernameNormalized, ct))
        {
            throw new ConflictException("username_taken", "This username is already taken.");
        }

        if (await db.Set<User>().AnyAsync(u => u.EmailNormalized == emailNormalized, ct))
        {
            throw new ConflictException("email_taken", "An account with this email already exists.");
        }

        var user = new User
        {
            Username = username,
            UsernameNormalized = usernameNormalized,
            Email = request.Email.Trim(),
            EmailNormalized = emailNormalized,
            DisplayName = displayName!,
            CreatedAt = clock.GetUtcNow().UtcDateTime,
        };
        user.PasswordHash = hasher.HashPassword(user, request.Password);

        db.Set<User>().Add(user);
        await db.SaveChangesAsync(ct);
        return user;
    }
}
