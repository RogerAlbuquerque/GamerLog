using GameLog.Application.Reviews;
using GameLog.Domain.Enums;

namespace GameLog.Application.Collections;

public sealed record CollectionSummaryDto(
    Guid Id,
    ReviewAuthorDto Owner,
    string Name,
    string? Description,
    string? CoverUrl,
    Visibility Visibility,
    int GameCount,
    IReadOnlyList<string> PreviewCovers,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record CollectionGameDto(Guid GameId, string Slug, string Title, string? CoverUrl, int? ReleaseYear, int Position);

public sealed record CollectionDetailDto(CollectionSummaryDto Collection, IReadOnlyList<CollectionGameDto> Games);

public sealed record SaveCollectionRequest(string Name, string? Description, Visibility Visibility);

public sealed record AddCollectionGameRequest(Guid GameId);

/// <summary>The complete new order: every game currently in the collection, exactly once.</summary>
public sealed record ReorderCollectionRequest(IReadOnlyList<Guid> GameIds);

public sealed record CollectionCoverResponse(string? CoverUrl);
