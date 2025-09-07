using GameLog.Domain.Entities;

namespace GameLog.Application.Games;

public sealed record NamedItem(int Id, string Name, string Slug);

public sealed record GameSummary(
    Guid Id,
    string Slug,
    string Title,
    string? CoverUrl,
    DateOnly? ReleaseDate,
    double? AverageRating,
    int RatingCount,
    IReadOnlyList<string> Genres,
    IReadOnlyList<string> Platforms);

public sealed record GameDetail(
    Guid Id,
    string Slug,
    string Title,
    string? Synopsis,
    string? CoverUrl,
    string? BackgroundUrl,
    DateOnly? ReleaseDate,
    string? Developer,
    string? Publisher,
    string? OfficialUrl,
    IReadOnlyList<string> GameModes,
    IReadOnlyList<NamedItem> Genres,
    IReadOnlyList<NamedItem> Platforms,
    double? AverageRating,
    int RatingCount,
    int CompletedCount);

/// <param name="Sort">title | release | rating | popular (default: popular)</param>
public sealed record GameSearchQuery(
    string? Q = null,
    string? Genre = null,
    string? Platform = null,
    int? Year = null,
    string? Sort = null,
    int? Page = null,
    int? PageSize = null);

public static class GameSort
{
    public const string Title = "title";
    public const string Release = "release";
    public const string Rating = "rating";
    public const string Popular = "popular";
}
