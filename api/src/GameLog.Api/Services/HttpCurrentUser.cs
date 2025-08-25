using System.Security.Claims;
using GameLog.Application.Abstractions;
using GameLog.Domain.Enums;

namespace GameLog.Api.Services;

/// <summary>Reads the caller from the validated authentication cookie; nothing here comes from request bodies.</summary>
public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid? UserId =>
        Guid.TryParse(accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public bool IsAdmin => accessor.HttpContext?.User.IsInRole(nameof(UserRole.Admin)) ?? false;
}
