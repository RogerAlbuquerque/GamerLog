using GameLog.Api.Extensions;
using GameLog.Application.Abstractions;
using GameLog.Application.Common;
using GameLog.Application.Reviews;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GameLog.Api.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class ReviewsController(ReviewService reviews, ICurrentUser current) : ControllerBase
{
    [HttpGet("games/{gameId:guid}/reviews")]
    [AllowAnonymous]
    public Task<PagedResult<ReviewDto>> ForGame(Guid gameId, string? sort, int? page, int? pageSize, CancellationToken ct) =>
        reviews.ListForGameAsync(gameId, sort, current.UserId, page, pageSize, ct);

    [HttpGet("users/{username}/reviews")]
    [AllowAnonymous]
    public Task<PagedResult<ReviewDto>> ForUser(string username, string? sort, int? page, int? pageSize, CancellationToken ct) =>
        reviews.ListForUserAsync(username, sort, current.UserId, page, pageSize, ct);

    [HttpGet("reviews/{id:guid}")]
    [AllowAnonymous]
    public Task<ReviewDto> Get(Guid id, CancellationToken ct) => reviews.GetAsync(id, current.UserId, ct);

    /// <summary>Creates or replaces the caller's review of the game (one per user and game).</summary>
    [HttpPut("games/{gameId:guid}/review")]
    [EnableRateLimiting(RateLimitingExtensions.Write)]
    public Task<ReviewDto> Save(Guid gameId, SaveReviewRequest request, CancellationToken ct) =>
        reviews.SaveAsync(current.RequireUserId(), gameId, request, ct);

    [HttpDelete("reviews/{id:guid}")]
    [EnableRateLimiting(RateLimitingExtensions.Write)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await reviews.DeleteAsync(current.RequireUserId(), current.IsAdmin, id, ct);
        return NoContent();
    }

    [HttpPut("reviews/{id:guid}/like")]
    [EnableRateLimiting(RateLimitingExtensions.Write)]
    public Task<LikeResponse> Like(Guid id, CancellationToken ct) => reviews.LikeAsync(current.RequireUserId(), id, ct);

    [HttpDelete("reviews/{id:guid}/like")]
    [EnableRateLimiting(RateLimitingExtensions.Write)]
    public Task<LikeResponse> Unlike(Guid id, CancellationToken ct) => reviews.UnlikeAsync(current.RequireUserId(), id, ct);
}
