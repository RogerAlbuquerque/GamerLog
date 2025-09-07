using GameLog.Application.Common;
using GameLog.Application.Games;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameLog.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1")]
public sealed class GamesController(GameService games) : ControllerBase
{
    /// <summary>Search the catalog by title, genre, platform and release year.</summary>
    [HttpGet("games")]
    public Task<PagedResult<GameSummary>> Search([FromQuery] GameSearchQuery query, CancellationToken ct) =>
        games.SearchAsync(query, ct);

    [HttpGet("games/{slug}")]
    public Task<GameDetail> Get(string slug, CancellationToken ct) => games.GetBySlugAsync(slug, ct);

    [HttpGet("games/{slug}/related")]
    public Task<IReadOnlyList<GameSummary>> Related(string slug, CancellationToken ct) => games.RelatedAsync(slug, 8, ct);

    [HttpGet("genres")]
    public Task<IReadOnlyList<NamedItem>> Genres(CancellationToken ct) => games.GenresAsync(ct);

    [HttpGet("platforms")]
    public Task<IReadOnlyList<NamedItem>> Platforms(CancellationToken ct) => games.PlatformsAsync(ct);
}
