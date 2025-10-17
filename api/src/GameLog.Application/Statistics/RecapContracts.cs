namespace GameLog.Application.Statistics;

public sealed record RecapGameDto(Guid GameId, string Slug, string Title, string? CoverUrl, int PlaytimeMinutes, int? HalfStars, DateOnly? CompletedOn);

public sealed record YearRecapDto(
    int Year,
    int GamesCompleted,
    int Completions,
    int TotalPlaytimeMinutes,
    double? AverageRating,
    IReadOnlyList<int> CompletedByMonth,
    int? BusiestMonth,
    IReadOnlyList<NamedCount> TopGenres,
    IReadOnlyList<NamedCount> TopPlatforms,
    RecapGameDto? LongestGame,
    RecapGameDto? ShortestGame,
    RecapGameDto? FirstCompleted,
    RecapGameDto? LastCompleted,
    IReadOnlyList<RecapGameDto> HighestRated,
    int ReviewsWritten,
    int RatingsGiven,
    IReadOnlyList<int> AvailableYears);
