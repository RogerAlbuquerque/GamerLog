using GameLog.Api.Extensions;
using GameLog.Application.Abstractions;
using GameLog.Application.Common;
using GameLog.Application.Social;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GameLog.Api.Controllers;

[ApiController]
[Route("api/v1/users/{username}")]
public sealed class FollowsController(FollowService follows, ICurrentUser current) : ControllerBase
{
    [HttpGet("follow")]
    [AllowAnonymous]
    public Task<FollowStateDto> State(string username, CancellationToken ct) => follows.StateAsync(username, current.UserId, ct);

    [HttpPut("follow")]
    [EnableRateLimiting(RateLimitingExtensions.Write)]
    public Task<FollowStateDto> Follow(string username, CancellationToken ct) => follows.FollowAsync(current.RequireUserId(), username, ct);

    [HttpDelete("follow")]
    [EnableRateLimiting(RateLimitingExtensions.Write)]
    public Task<FollowStateDto> Unfollow(string username, CancellationToken ct) => follows.UnfollowAsync(current.RequireUserId(), username, ct);

    [HttpGet("followers")]
    [AllowAnonymous]
    public Task<PagedResult<UserListItemDto>> Followers(string username, int? page, int? pageSize, CancellationToken ct) =>
        follows.FollowersAsync(username, current.UserId, page, pageSize, ct);

    [HttpGet("following")]
    [AllowAnonymous]
    public Task<PagedResult<UserListItemDto>> Following(string username, int? page, int? pageSize, CancellationToken ct) =>
        follows.FollowingAsync(username, current.UserId, page, pageSize, ct);
}
