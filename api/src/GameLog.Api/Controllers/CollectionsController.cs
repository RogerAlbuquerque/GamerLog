using GameLog.Api.Extensions;
using GameLog.Application.Abstractions;
using GameLog.Application.Collections;
using GameLog.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GameLog.Api.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class CollectionsController(CollectionService collections, ICurrentUser current) : ControllerBase
{
    private const long MaxUploadBytes = 6 * 1024 * 1024;

    [HttpGet("collections")]
    [AllowAnonymous]
    public Task<PagedResult<CollectionSummaryDto>> Public(int? page, int? pageSize, CancellationToken ct) =>
        collections.ListPublicAsync(page, pageSize, ct);

    [HttpGet("users/{username}/collections")]
    [AllowAnonymous]
    public Task<PagedResult<CollectionSummaryDto>> ForUser(string username, int? page, int? pageSize, CancellationToken ct) =>
        collections.ListForUserAsync(username, current.UserId, page, pageSize, ct);

    [HttpGet("collections/{id:guid}")]
    [AllowAnonymous]
    public Task<CollectionDetailDto> Get(Guid id, CancellationToken ct) => collections.GetAsync(id, current.UserId, ct);

    [HttpPost("collections")]
    [EnableRateLimiting(RateLimitingExtensions.Write)]
    public async Task<ActionResult<CollectionDetailDto>> Create(SaveCollectionRequest request, CancellationToken ct)
    {
        var created = await collections.CreateAsync(current.RequireUserId(), request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Collection.Id }, created);
    }

    [HttpPut("collections/{id:guid}")]
    [EnableRateLimiting(RateLimitingExtensions.Write)]
    public Task<CollectionDetailDto> Update(Guid id, SaveCollectionRequest request, CancellationToken ct) =>
        collections.UpdateAsync(current.RequireUserId(), id, request, ct);

    [HttpDelete("collections/{id:guid}")]
    [EnableRateLimiting(RateLimitingExtensions.Write)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await collections.DeleteAsync(current.RequireUserId(), current.IsAdmin, id, ct);
        return NoContent();
    }

    [HttpPost("collections/{id:guid}/games")]
    [EnableRateLimiting(RateLimitingExtensions.Write)]
    public Task<CollectionDetailDto> AddGame(Guid id, AddCollectionGameRequest request, CancellationToken ct) =>
        collections.AddGameAsync(current.RequireUserId(), id, request.GameId, ct);

    [HttpDelete("collections/{id:guid}/games/{gameId:guid}")]
    [EnableRateLimiting(RateLimitingExtensions.Write)]
    public Task<CollectionDetailDto> RemoveGame(Guid id, Guid gameId, CancellationToken ct) =>
        collections.RemoveGameAsync(current.RequireUserId(), id, gameId, ct);

    [HttpPut("collections/{id:guid}/order")]
    [EnableRateLimiting(RateLimitingExtensions.Write)]
    public Task<CollectionDetailDto> Reorder(Guid id, ReorderCollectionRequest request, CancellationToken ct) =>
        collections.ReorderAsync(current.RequireUserId(), id, request.GameIds, ct);

    [HttpPut("collections/{id:guid}/cover")]
    [RequestSizeLimit(MaxUploadBytes)]
    [EnableRateLimiting(RateLimitingExtensions.Upload)]
    public async Task<CollectionCoverResponse> SetCover(Guid id, IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        return await collections.SetCoverAsync(current.RequireUserId(), id, stream, ct);
    }

    [HttpDelete("collections/{id:guid}/cover")]
    [EnableRateLimiting(RateLimitingExtensions.Write)]
    public async Task<IActionResult> RemoveCover(Guid id, CancellationToken ct)
    {
        await collections.RemoveCoverAsync(current.RequireUserId(), id, ct);
        return NoContent();
    }
}
