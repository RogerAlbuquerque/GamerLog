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

    public const int MaxFailedAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    // Verified against when the account does not exist, so response time does not reveal which usernames are registered.
    private static readonly string DummyHash = new PasswordHasher<User>().HashPassword(new User(), Guid.NewGuid().ToString("N"));

    /// <summary>
    /// Verifies credentials. Every failure looks the same to the caller (401) except lockout (429).
    /// Five consecutive failures lock the account for 15 minutes.
    /// </summary>
    public async Task<User> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var login = (request.Login ?? "").Trim().ToLowerInvariant();
        var password = request.Password ?? "";
        var user = login.Length == 0
            ? null
            : await db.Set<User>().FirstOrDefaultAsync(u => u.UsernameNormalized == login || u.EmailNormalized == login, ct);
        var now = clock.GetUtcNow().UtcDateTime;

        if (user is null)
        {
            hasher.VerifyHashedPassword(new User(), DummyHash, password);
            throw new UnauthorizedException("Invalid username or password.");
        }

        if (user.LockoutEnd is { } lockedUntil && lockedUntil > now)
        {
            throw new AccountLockedException();
        }

        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
        {
            user.FailedLoginCount++;
            if (user.FailedLoginCount >= MaxFailedAttempts)
            {
                user.LockoutEnd = now + LockoutDuration;
                user.FailedLoginCount = 0;
            }

            await db.SaveChangesAsync(ct);
            throw new UnauthorizedException("Invalid username or password.");
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = hasher.HashPassword(user, password);
        }

        user.FailedLoginCount = 0;
        user.LockoutEnd = null;
        await db.SaveChangesAsync(ct);
        return user;
    }
}
