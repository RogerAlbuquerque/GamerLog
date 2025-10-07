using GameLog.Application.Abstractions;
using GameLog.Application.Activity;
using GameLog.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameLog.Api.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class ActivityController(ActivityService activity, ICurrentUser current) : ControllerBase
{
    /// <summary>The caller's home feed: their own activity plus the people they follow.</summary>
    [HttpGet("feed")]
    public Task<PagedResult<ActivityDto>> Feed(int? page, int? pageSize, CancellationToken ct) =>
        activity.FeedAsync(current.RequireUserId(), page, pageSize, ct);

    [HttpGet("users/{username}/activity")]
    [AllowAnonymous]
    public Task<PagedResult<ActivityDto>> ForUser(string username, int? page, int? pageSize, CancellationToken ct) =>
        activity.ForUserAsync(username, page, pageSize, ct);
}
