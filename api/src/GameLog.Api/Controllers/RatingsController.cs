using GameLog.Api.Extensions;
using GameLog.Application.Abstractions;
using GameLog.Application.Ratings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GameLog.Api.Controllers;

[ApiController]
[Route("api/v1/games/{gameId:guid}")]
public sealed class RatingsController(RatingService ratings, ICurrentUser current) : ControllerBase
{
    /// <summary>Community average, number of ratings and the 10-bucket distribution.</summary>
    [HttpGet("rating-summary")]
    [AllowAnonymous]
    public Task<RatingSummaryDto> Summary(Guid gameId, CancellationToken ct) => ratings.SummaryAsync(gameId, ct);

    [HttpGet("rating")]
    public Task<RatingDto> Mine(Guid gameId, CancellationToken ct) => ratings.GetMineAsync(current.RequireUserId(), gameId, ct);

    [HttpPut("rating")]
    [EnableRateLimiting(RateLimitingExtensions.Write)]
    public Task<RatingDto> Set(Guid gameId, SetRatingRequest request, CancellationToken ct) =>
        ratings.SetAsync(current.RequireUserId(), gameId, request.HalfStars, ct);

    [HttpDelete("rating")]
    [EnableRateLimiting(RateLimitingExtensions.Write)]
    public async Task<IActionResult> Remove(Guid gameId, CancellationToken ct)
    {
        await ratings.RemoveAsync(current.RequireUserId(), gameId, ct);
        return NoContent();
    }
}
