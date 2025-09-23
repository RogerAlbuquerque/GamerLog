using GameLog.Domain.Enums;

namespace GameLog.Application.Diary;

public sealed record PlaythroughDto(
    Guid Id, int Number, DateOnly? StartedOn, DateOnly? CompletedOn, int PlaytimeMinutes, int? PlatformId, string? Platform, string? Notes);

public sealed record DiaryEntryDto(
    Guid Id,
    Guid GameId,
    string GameSlug,
    string GameTitle,
    string? CoverUrl,
    DiaryStatus Status,
    int? PlatformId,
    string? Platform,
    int TotalPlaytimeMinutes,
    int? RatingHalfStars,
    bool? Recommended,
    string? Notes,
    DateOnly? StartedOn,
    DateOnly? CompletedOn,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<PlaythroughDto> Playthroughs);

/// <summary>
/// Creates or updates the caller's diary entry for a game. The dates and playtime describe the
/// latest playthrough; a new one is created automatically when needed. <see cref="Notes"/> and
/// <see cref="Recommended"/> belong to the entry as a whole.
/// </summary>
public sealed record UpsertDiaryRequest(
    DiaryStatus Status,
    int? PlatformId,
    DateOnly? StartedOn,
    DateOnly? CompletedOn,
    int? PlaytimeMinutes,
    bool? Recommended,
    string? Notes);

public sealed record PlaythroughRequest(
    DateOnly? StartedOn, DateOnly? CompletedOn, int? PlaytimeMinutes, int? PlatformId, string? Notes);
