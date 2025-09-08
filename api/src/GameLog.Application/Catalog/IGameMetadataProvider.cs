using GameLog.Application.Common;

namespace GameLog.Application.Catalog;

/// <summary>A game as described by an external database, before it is stored locally.</summary>
public sealed record ExternalGame(
    string Provider,
    string ExternalId,
    string Title,
    string? Summary,
    DateOnly? ReleaseDate,
    string? Developer,
    string? Publisher,
    string? CoverUrl,
    string? BackgroundUrl,
    string? WebsiteUrl,
    IReadOnlyList<string> GameModes,
    IReadOnlyList<string> Genres,
    IReadOnlyList<string> Platforms);

/// <summary>
/// Boundary to an external game database (IGDB, RAWG, ...). It is used only by the import flow:
/// no user-facing request ever depends on it, so the app keeps working when the provider is down.
/// </summary>
public interface IGameMetadataProvider
{
    string Name { get; }

    Task<IReadOnlyList<ExternalGame>> SearchAsync(string query, int limit, CancellationToken ct = default);

    Task<ExternalGame?> GetAsync(string externalId, CancellationToken ct = default);
}

/// <summary>The provider is not configured, rejected our credentials, or could not be reached.</summary>
public sealed class ProviderUnavailableException(string message)
    : AppException(503, "provider_unavailable", message)
{
}
