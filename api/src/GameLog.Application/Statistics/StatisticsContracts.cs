using GameLog.Domain.Enums;

namespace GameLog.Application.Statistics;

/// <summary>Everything the statistics maths needs about one diary entry, already loaded into memory.</summary>
public sealed record DiaryStatRow(
    Guid GameId,
    string Slug,
    string Title,
    string? CoverUrl,
    DiaryStatus Status,
    int PlaytimeMinutes,
    string? Platform,
    IReadOnlyList<string> Genres,
    IReadOnlyList<PlaythroughStat> Playthroughs,
    int? HalfStars,
    int? ReleaseYear);

public sealed record PlaythroughStat(DateOnly? CompletedOn, int PlaytimeMinutes);

public sealed record NamedCount(string Name, int Count);

public sealed record GameStat(Guid GameId, string Slug, string Title, string? CoverUrl, int PlaytimeMinutes, int? HalfStars);

public sealed record StatisticsDto(
    int TotalGames,
    IReadOnlyDictionary<DiaryStatus, int> ByStatus,
    int CompletedGames,
    int TotalPlaytimeMinutes,
    double? AverageRating,
    int RatingCount,
    IReadOnlyList<int> RatingDistribution,
    IReadOnlyList<NamedCount> TopGenres,
    IReadOnlyList<NamedCount> TopPlatforms,
    IReadOnlyList<GameStat> MostPlayed,
    IReadOnlyList<GameStat> HighestRated,
    int ReviewCount,
    int CollectionCount);
