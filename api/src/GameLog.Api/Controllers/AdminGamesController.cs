using GameLog.Api.Extensions;
using GameLog.Application.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GameLog.Api.Controllers;

/// <summary>Catalog administration. Authorization is enforced by the Admin policy, which reads the role from the validated cookie.</summary>
[ApiController]
[Authorize(Policy = AuthExtensions.AdminPolicy)]
[Route("api/v1/admin/games")]
public sealed class AdminGamesController(GameImportService import) : ControllerBase
{
    public sealed record ImportRequest(string ExternalId);

    /// <summary>Search the external provider (IGDB) for games to import.</summary>
    [HttpGet("search")]
    public Task<IReadOnlyList<ExternalGame>> Search([FromQuery] string q, CancellationToken ct) =>
        import.SearchProviderAsync(q, ct);

    /// <summary>Copy one game from the provider into the local catalog (idempotent).</summary>
    [HttpPost("import")]
    [EnableRateLimiting(RateLimitingExtensions.Write)]
    public async Task<ImportedGame> Import(ImportRequest request, CancellationToken ct) =>
        await import.ImportAsync(request.ExternalId, ct);
}
