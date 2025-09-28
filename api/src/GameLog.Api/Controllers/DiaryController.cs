using GameLog.Api.Extensions;
using GameLog.Application.Abstractions;
using GameLog.Application.Common;
using GameLog.Application.Diary;
using GameLog.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GameLog.Api.Controllers;

[ApiController]
[Route("api/v1")]
[EnableRateLimiting(RateLimitingExtensions.Write)]
public sealed class DiaryController(DiaryService diary, ICurrentUser current) : ControllerBase
{
    /// <summary>A user's public diary, optionally filtered by status.</summary>
    [HttpGet("users/{username}/diary")]
    [AllowAnonymous]
    public Task<PagedResult<DiaryEntryDto>> List(
        string username, [FromQuery] DiaryStatus? status, [FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken ct) =>
        diary.ListAsync(username, status, page, pageSize, ct);

    /// <summary>The caller's own entry for a game (404 when the game is not in the diary).</summary>
    [HttpGet("diary/{gameId:guid}")]
    public Task<DiaryEntryDto> Get(Guid gameId, CancellationToken ct) => diary.GetAsync(current.RequireUserId(), gameId, ct);

    [HttpPut("diary/{gameId:guid}")]
    public Task<DiaryEntryDto> Upsert(Guid gameId, UpsertDiaryRequest request, CancellationToken ct) =>
        diary.UpsertAsync(current.RequireUserId(), gameId, request, ct);

    [HttpDelete("diary/{gameId:guid}")]
    public async Task<IActionResult> Delete(Guid gameId, CancellationToken ct)
    {
        await diary.DeleteAsync(current.RequireUserId(), gameId, ct);
        return NoContent();
    }

    [HttpPost("diary/{gameId:guid}/playthroughs")]
    public async Task<ActionResult<DiaryEntryDto>> AddPlaythrough(Guid gameId, PlaythroughRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await diary.AddPlaythroughAsync(current.RequireUserId(), gameId, request, ct));

    [HttpPatch("playthroughs/{id:guid}")]
    public Task<DiaryEntryDto> UpdatePlaythrough(Guid id, PlaythroughRequest request, CancellationToken ct) =>
        diary.UpdatePlaythroughAsync(current.RequireUserId(), id, request, ct);

    [HttpDelete("playthroughs/{id:guid}")]
    public Task<DiaryEntryDto> DeletePlaythrough(Guid id, CancellationToken ct) =>
        diary.DeletePlaythroughAsync(current.RequireUserId(), id, ct);
}
