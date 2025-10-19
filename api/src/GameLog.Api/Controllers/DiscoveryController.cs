using GameLog.Application.Abstractions;
using GameLog.Application.Discovery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameLog.Api.Controllers;

[ApiController]
[Route("api/v1/discover")]
public sealed class DiscoveryController(DiscoveryService discovery, ICurrentUser current) : ControllerBase
{
    /// <summary>Popular, top rated, new releases, most completed and trending rails.</summary>
    [HttpGet]
    [AllowAnonymous]
    public Task<DiscoveryDto> Overview(CancellationToken ct) => discovery.OverviewAsync(ct);

    /// <summary>Rule-based suggestions from the signed-in user's own diary and ratings.</summary>
    [HttpGet("recommended")]
    public Task<IReadOnlyList<RecommendedGameDto>> Recommended(int? take, CancellationToken ct) =>
        discovery.RecommendAsync(current.RequireUserId(), take ?? DiscoveryService.RailSize, ct);
}
