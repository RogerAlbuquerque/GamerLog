using GameLog.Api.Extensions;
using GameLog.Application.Abstractions;
using GameLog.Application.Auth;
using GameLog.Application.Common;
using GameLog.Application.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GameLog.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
public sealed class UsersController(ProfileService profiles, ICurrentUser current) : ControllerBase
{
    private const long MaxUploadBytes = 6 * 1024 * 1024;

    [HttpGet("{username}")]
    [AllowAnonymous]
    public Task<ProfileDto> Get(string username, CancellationToken ct) => profiles.GetAsync(username, ct);

    /// <summary>Edits the signed-in user's own profile. The target is always the caller, never a client-supplied id.</summary>
    [HttpPatch("me")]
    public Task<SessionUser> Update(UpdateProfileRequest request, CancellationToken ct) =>
        profiles.UpdateAsync(current.RequireUserId(), request, ct);

    [HttpPut("me/avatar")]
    [RequestSizeLimit(MaxUploadBytes)]
    [EnableRateLimiting(RateLimitingExtensions.Upload)]
    public async Task<AvatarResponse> SetAvatar(IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        return await profiles.SetAvatarAsync(current.RequireUserId(), stream, ct);
    }

    [HttpDelete("me/avatar")]
    public async Task<IActionResult> RemoveAvatar(CancellationToken ct)
    {
        await profiles.RemoveAvatarAsync(current.RequireUserId(), ct);
        return NoContent();
    }

    [HttpDelete("me")]
    [EnableRateLimiting(RateLimitingExtensions.Auth)]
    public async Task<IActionResult> DeleteAccount(DeleteAccountRequest request, CancellationToken ct)
    {
        await profiles.DeleteAccountAsync(current.RequireUserId(), request.Password, ct);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }
}
