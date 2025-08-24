using GameLog.Application.Abstractions;
using GameLog.Application.Common;
using GameLog.Domain.Entities;
using GameLog.Domain.Rules;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GameLog.Application.Auth;

/// <summary>Account-level operations of the signed-in user (session info, password change).</summary>
public sealed class AccountService(IAppDbContext db, IPasswordHasher<User> hasher)
{
    public async Task<User> GetAsync(Guid userId, CancellationToken ct = default) =>
        await db.Query<User>().FirstOrDefaultAsync(u => u.Id == userId, ct) ?? throw new NotFoundException();

    /// <summary>
    /// Changes the password and rotates the security stamp, which invalidates every other session.
    /// The caller must sign the user in again with the returned (new-stamp) user.
    /// </summary>
    public async Task<User> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await GetAsync(userId, ct);

        if (hasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword ?? "") == PasswordVerificationResult.Failed)
        {
            throw new ValidationFailedException("currentPassword", "The current password is incorrect.");
        }

        if (UserRules.ValidatePassword(request.NewPassword, user.Username) is { } error)
        {
            throw new ValidationFailedException("newPassword", error);
        }

        user.PasswordHash = hasher.HashPassword(user, request.NewPassword);
        user.SecurityStamp = Guid.NewGuid();
        await db.SaveChangesAsync(ct);
        return user;
    }
}
