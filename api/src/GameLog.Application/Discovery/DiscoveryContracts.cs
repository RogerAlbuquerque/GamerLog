using GameLog.Application.Games;

namespace GameLog.Application.Discovery;

public sealed record DiscoveryDto(
    IReadOnlyList<GameSummary> Popular,
    IReadOnlyList<GameSummary> TopRated,
    IReadOnlyList<GameSummary> NewReleases,
    IReadOnlyList<GameSummary> MostCompleted,
    IReadOnlyList<GameSummary> Trending);

public sealed record RecommendedGameDto(GameSummary Game, double Score, IReadOnlyList<string> Because);
