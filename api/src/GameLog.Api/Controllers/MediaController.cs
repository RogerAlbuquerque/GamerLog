using GameLog.Application.Abstractions;
using GameLog.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameLog.Api.Controllers;

/// <summary>
/// Serves stored images. The bucket is private, so every read goes through here and the key is
/// validated before it reaches the storage layer.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/media")]
public sealed class MediaController(IObjectStorage storage) : ControllerBase
{
    [HttpGet("{**key}")]
    public async Task<IActionResult> Get(string key, CancellationToken ct)
    {
        if (!ObjectKeys.IsValid(key))
        {
            return NotFound();
        }

        var stored = await storage.GetAsync(key, ct);
        if (stored is null)
        {
            return NotFound();
        }

        // Keys are random and content is immutable, so browsers and CDNs may cache for a year.
        Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        return File(stored.Content, stored.ContentType);
    }
}
