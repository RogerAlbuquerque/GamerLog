using GameLog.Api.Extensions;
using GameLog.Application.Abstractions;
using GameLog.Application.Comments;
using GameLog.Application.Common;
using GameLog.Application.Reviews;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GameLog.Api.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class CommentsController(CommentService comments, ICurrentUser current) : ControllerBase
{
    [HttpGet("games/{gameId:guid}/comments")]
    [AllowAnonymous]
    public Task<PagedResult<CommentDto>> ForGame(Guid gameId, string? sort, int? page, int? pageSize, CancellationToken ct) =>
        comments.ListAsync(CommentTarget.Game(gameId), sort, current.UserId, page, pageSize, ct);

    [HttpGet("reviews/{reviewId:guid}/comments")]
    [AllowAnonymous]
    public Task<PagedResult<CommentDto>> ForReview(Guid reviewId, string? sort, int? page, int? pageSize, CancellationToken ct) =>
        comments.ListAsync(CommentTarget.Review(reviewId), sort, current.UserId, page, pageSize, ct);

    [HttpGet("comments/{id:guid}/replies")]
    [AllowAnonymous]
    public Task<PagedResult<CommentDto>> Replies(Guid id, int? page, int? pageSize, CancellationToken ct) =>
        comments.RepliesAsync(id, current.UserId, page, pageSize, ct);

    [HttpPost("games/{gameId:guid}/comments")]
    [EnableRateLimiting(RateLimitingExtensions.Write)]
    public async Task<ActionResult<CommentDto>> CreateOnGame(Guid gameId, CreateCommentRequest request, CancellationToken ct) =>
        Created(string.Empty, await comments.CreateAsync(current.RequireUserId(), CommentTarget.Game(gameId), request, ct));

    [HttpPost("reviews/{reviewId:guid}/comments")]
    [EnableRateLimiting(RateLimitingExtensions.Write)]
    public async Task<ActionResult<CommentDto>> CreateOnReview(Guid reviewId, CreateCommentRequest request, CancellationToken ct) =>
        Created(string.Empty, await comments.CreateAsync(current.RequireUserId(), CommentTarget.Review(reviewId), request, ct));

    [HttpPatch("comments/{id:guid}")]
    [EnableRateLimiting(RateLimitingExtensions.Write)]
    public Task<CommentDto> Edit(Guid id, EditCommentRequest request, CancellationToken ct) =>
        comments.EditAsync(current.RequireUserId(), id, request, ct);

    [HttpDelete("comments/{id:guid}")]
    [EnableRateLimiting(RateLimitingExtensions.Write)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await comments.DeleteAsync(current.RequireUserId(), current.IsAdmin, id, ct);
        return NoContent();
    }

    [HttpPut("comments/{id:guid}/like")]
    [EnableRateLimiting(RateLimitingExtensions.Write)]
    public Task<LikeResponse> Like(Guid id, CancellationToken ct) => comments.LikeAsync(current.RequireUserId(), id, ct);

    [HttpDelete("comments/{id:guid}/like")]
    [EnableRateLimiting(RateLimitingExtensions.Write)]
    public Task<LikeResponse> Unlike(Guid id, CancellationToken ct) => comments.UnlikeAsync(current.RequireUserId(), id, ct);
}
