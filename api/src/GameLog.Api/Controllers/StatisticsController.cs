using GameLog.Application.Statistics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameLog.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/users/{username}")]
public sealed class StatisticsController(StatisticsService statistics, TimeProvider clock) : ControllerBase
{
    [HttpGet("stats")]
    public Task<StatisticsDto> Stats(string username, CancellationToken ct) => statistics.GetAsync(username, ct);

    /// <summary>The yearly retrospective; defaults to the current year.</summary>
    [HttpGet("recap")]
    public Task<YearRecapDto> Recap(string username, int? year, CancellationToken ct) =>
        statistics.RecapAsync(username, year ?? clock.GetUtcNow().Year, ct);
}
