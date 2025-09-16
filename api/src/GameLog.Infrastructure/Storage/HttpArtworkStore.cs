using GameLog.Application.Abstractions;
using GameLog.Application.Common;
using Microsoft.Extensions.Logging;

namespace GameLog.Infrastructure.Storage;

/// <summary>
/// Copies provider artwork into our own storage. Because the URL comes from a third party, it is
/// treated as hostile: https only, host allow-list, no redirects, bounded size (SSRF protection).
/// </summary>
public sealed class HttpArtworkStore(
    HttpClient http,
    IImageProcessor images,
    IObjectStorage storage,
    ILogger<HttpArtworkStore> logger) : IGameArtworkStore
{
    public static readonly string[] AllowedHosts = ["images.igdb.com"];

    public static bool IsAllowed(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps
        && uri.IsDefaultPort
        && string.IsNullOrEmpty(uri.UserInfo)
        && AllowedHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase);

    public async Task<string?> StoreAsync(string? url, ImageKind kind, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        if (!IsAllowed(url))
        {
            logger.LogWarning("Refusing to download artwork from a non-allow-listed URL");
            return null;
        }

        try
        {
            using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Artwork download failed with {Status}", (int)response.StatusCode);
                return null;
            }

            await using var body = await response.Content.ReadAsStreamAsync(ct);
            var processed = await images.ProcessAsync(body, kind, ct); // enforces the size limit while reading
            var key = ObjectKeys.New("games", processed.Extension);
            await storage.PutAsync(key, new MemoryStream(processed.Data), processed.ContentType, ct);
            return key;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or ValidationFailedException)
        {
            logger.LogWarning(ex, "Could not copy artwork; the game will use generated art");
            return null;
        }
    }
}
