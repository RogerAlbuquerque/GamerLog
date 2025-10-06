using GameLog.Api.Extensions;
using GameLog.Application.Abstractions;
using GameLog.Application.Common;
using GameLog.Application.Notifications;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GameLog.Api.Controllers;

/// <summary>Every endpoint acts on the caller's own notifications; there is no user id in any route.</summary>
[ApiController]
[Route("api/v1/notifications")]
public sealed class NotificationsController(NotificationService notifications, ICurrentUser current) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<NotificationDto>> List(bool unreadOnly, int? page, int? pageSize, CancellationToken ct) =>
        notifications.ListAsync(current.RequireUserId(), unreadOnly, page, pageSize, ct);

    [HttpGet("unread-count")]
    public Task<UnreadCountDto> Unread(CancellationToken ct) => notifications.UnreadCountAsync(current.RequireUserId(), ct);

    [HttpPost("{id:guid}/read")]
    [EnableRateLimiting(RateLimitingExtensions.Write)]
    public async Task<IActionResult> Read(Guid id, CancellationToken ct)
    {
        await notifications.MarkReadAsync(current.RequireUserId(), id, ct);
        return NoContent();
    }

    [HttpPost("read-all")]
    [EnableRateLimiting(RateLimitingExtensions.Write)]
    public async Task<IActionResult> ReadAll(CancellationToken ct)
    {
        await notifications.MarkAllReadAsync(current.RequireUserId(), ct);
        return NoContent();
    }
}
