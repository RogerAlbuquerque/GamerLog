using GameLog.Application.Abstractions;
using GameLog.Application.Auth;
using GameLog.Application.Common;
using GameLog.Domain.Entities;
using GameLog.Domain.Rules;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameLog.Application.Users;

public sealed class ProfileService(
    IAppDbContext db,
    IObjectStorage storage,
    IImageProcessor images,
    IPasswordHasher<User> hasher,
    ILogger<ProfileService> logger)
{
    public async Task<ProfileDto> GetAsync(string username, CancellationToken ct = default)
    {
        var key = UserRules.NormalizeUsername(username ?? "");
        var user = await db.ReadOnly<User>().FirstOrDefaultAsync(u => u.UsernameNormalized == key, ct)
                   ?? throw new NotFoundException("User not found.");
        return new ProfileDto(user.Id, user.Username, user.DisplayName, user.Bio, MediaUrls.For(user.AvatarKey), user.CreatedAt);
    }

    public async Task<SessionUser> UpdateAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var user = await LoadAsync(userId, ct);
        var errors = new Dictionary<string, string[]>();

        if (request.DisplayName is not null)
        {
            if (UserRules.ValidateDisplayName(request.DisplayName) is { } e) errors["displayName"] = [e];
            else user.DisplayName = request.DisplayName.Trim();
        }

        if (request.Bio is not null)
        {
            if (UserRules.ValidateBio(request.Bio) is { } e) errors["bio"] = [e];
            else user.Bio = string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio.Trim();
        }

        if (errors.Count > 0)
        {
            throw new ValidationFailedException(errors);
        }

        if (request.ShowSpoilersByDefault is { } show)
        {
            user.ShowSpoilersByDefault = show;
        }

        await db.SaveChangesAsync(ct);
        return SessionUser.From(user);
    }

    /// <summary>Validates and re-encodes the upload, then swaps the stored avatar and removes the old file.</summary>
    public async Task<AvatarResponse> SetAvatarAsync(Guid userId, Stream upload, CancellationToken ct = default)
    {
        var user = await LoadAsync(userId, ct);
        var processed = await images.ProcessAsync(upload, ImageKind.Avatar, ct);

        var newKey = ObjectKeys.New("avatars", processed.Extension);
        await storage.PutAsync(newKey, new MemoryStream(processed.Data), processed.ContentType, ct);

        var oldKey = user.AvatarKey;
        user.AvatarKey = newKey;
        await db.SaveChangesAsync(ct);

        await TryDeleteAsync(oldKey);
        return new AvatarResponse(MediaUrls.For(newKey));
    }

    public async Task RemoveAvatarAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await LoadAsync(userId, ct);
        var oldKey = user.AvatarKey;
        user.AvatarKey = null;
        await db.SaveChangesAsync(ct);
        await TryDeleteAsync(oldKey);
    }

    /// <summary>Permanently deletes the account after re-checking the password. Dependent rows are removed by cascade.</summary>
    public async Task DeleteAccountAsync(Guid userId, string password, CancellationToken ct = default)
    {
        var user = await LoadAsync(userId, ct);
        if (hasher.VerifyHashedPassword(user, user.PasswordHash, password ?? "") == PasswordVerificationResult.Failed)
        {
            throw new ValidationFailedException("password", "The password is incorrect.");
        }

        var avatarKey = user.AvatarKey;
        db.Set<User>().Remove(user);
        await db.SaveChangesAsync(ct);
        await TryDeleteAsync(avatarKey);
        logger.LogInformation("User {UserId} deleted their account", userId);
    }

    private async Task<User> LoadAsync(Guid userId, CancellationToken ct) =>
        await db.Query<User>().FirstOrDefaultAsync(u => u.Id == userId, ct) ?? throw new NotFoundException("User not found.");

    /// <summary>A leftover file is harmless; failing the request over it would not be.</summary>
    private async Task TryDeleteAsync(string? key)
    {
        if (key is null)
        {
            return;
        }

        try
        {
            await storage.DeleteAsync(key);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not delete stored object {Key}", key);
        }
    }
}
