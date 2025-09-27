using GameLog.Application.Common;
using GameLog.Domain.Entities;
using GameLog.Domain.Enums;
using GameLog.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace GameLog.Application.Diary;

public sealed partial class DiaryService
{
    /// <summary>Adds a replay to a game that is already in the diary and past the wishlist stage.</summary>
    public async Task<DiaryEntryDto> AddPlaythroughAsync(Guid userId, Guid gameId, PlaythroughRequest r, CancellationToken ct = default)
    {
        var entry = await db.Query<GamingDiaryEntry>().Include(e => e.Playthroughs)
                        .FirstOrDefaultAsync(e => e.UserId == userId && e.GameId == gameId, ct)
                    ?? throw new NotFoundException("This game is not in your diary.");

        if (entry.Status == DiaryStatus.WantToPlay)
        {
            throw new ValidationFailedException("status", "Start the game before adding playthroughs.");
        }

        var status = r.CompletedOn is null ? DiaryStatus.Playing : DiaryStatus.Completed;
        await ValidatePlaythroughAsync(status, r, ct);

        var playthrough = new Playthrough
        {
            DiaryEntryId = entry.Id,
            Number = entry.Playthroughs.Count == 0 ? 1 : entry.Playthroughs.Max(p => p.Number) + 1,
            StartedOn = r.StartedOn,
            CompletedOn = r.CompletedOn,
            PlaytimeMinutes = r.PlaytimeMinutes ?? 0,
            PlatformId = r.PlatformId,
            Notes = Clean(r.Notes),
        };
        entry.Playthroughs.Add(playthrough);
        db.Set<Playthrough>().Add(playthrough);

        entry.Status = status;
        entry.TotalPlaytimeMinutes = TotalPlaytime(entry);
        entry.UpdatedAt = clock.GetUtcNow().UtcDateTime;
        await PersistAsync(gameId, ct);
        return await GetAsync(userId, gameId, ct);
    }

    public async Task<DiaryEntryDto> UpdatePlaythroughAsync(Guid userId, Guid playthroughId, PlaythroughRequest r, CancellationToken ct = default)
    {
        var (entry, playthrough) = await LoadOwnedAsync(userId, playthroughId, ct);
        var isLatest = playthrough.Number == entry.Playthroughs.Max(p => p.Number);

        // Completing or reopening the latest run keeps the entry's status in step with it.
        var status = isLatest ? (r.CompletedOn is null ? (entry.Status == DiaryStatus.Completed ? DiaryStatus.Playing : entry.Status) : DiaryStatus.Completed) : entry.Status;
        await ValidatePlaythroughAsync(r.CompletedOn is null ? DiaryStatus.Playing : DiaryStatus.Completed, r, ct);

        playthrough.StartedOn = r.StartedOn;
        playthrough.CompletedOn = r.CompletedOn;
        playthrough.PlaytimeMinutes = r.PlaytimeMinutes ?? playthrough.PlaytimeMinutes;
        playthrough.PlatformId = r.PlatformId;
        playthrough.Notes = Clean(r.Notes);

        entry.Status = status;
        entry.TotalPlaytimeMinutes = TotalPlaytime(entry);
        entry.UpdatedAt = clock.GetUtcNow().UtcDateTime;
        await PersistAsync(entry.GameId, ct);
        return await GetAsync(userId, entry.GameId, ct);
    }

    public async Task<DiaryEntryDto> DeletePlaythroughAsync(Guid userId, Guid playthroughId, CancellationToken ct = default)
    {
        var (entry, playthrough) = await LoadOwnedAsync(userId, playthroughId, ct);
        if (entry.Playthroughs.Count == 1)
        {
            throw new ValidationFailedException("playthrough", "An entry needs at least one playthrough. Remove the game from your diary instead.");
        }

        entry.Playthroughs.Remove(playthrough);
        db.Set<Playthrough>().Remove(playthrough);
        entry.TotalPlaytimeMinutes = TotalPlaytime(entry);
        entry.UpdatedAt = clock.GetUtcNow().UtcDateTime;
        await PersistAsync(entry.GameId, ct);
        return await GetAsync(userId, entry.GameId, ct);
    }

    /// <summary>Someone else's playthrough looks exactly like a missing one: 404, never 403.</summary>
    private async Task<(GamingDiaryEntry Entry, Playthrough Playthrough)> LoadOwnedAsync(Guid userId, Guid playthroughId, CancellationToken ct)
    {
        var entry = await db.Query<GamingDiaryEntry>().Include(e => e.Playthroughs)
                        .FirstOrDefaultAsync(e => e.UserId == userId && e.Playthroughs.Any(p => p.Id == playthroughId), ct)
                    ?? throw new NotFoundException("Playthrough not found.");
        return (entry, entry.Playthroughs.Single(p => p.Id == playthroughId));
    }

    private async Task ValidatePlaythroughAsync(DiaryStatus status, PlaythroughRequest r, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();
        foreach (var v in DiaryRules.Validate(status, r.StartedOn, r.CompletedOn, r.PlaytimeMinutes ?? 0, Today))
        {
            errors.TryAdd(v.Field, [v.Message]);
        }

        if (r.Notes is { Length: > MaxNotesLength })
        {
            errors["notes"] = [$"Notes must be at most {MaxNotesLength} characters."];
        }

        if (r.PlatformId is { } platformId && !await db.ReadOnly<Platform>().AnyAsync(p => p.Id == platformId, ct))
        {
            errors["platformId"] = ["Unknown platform."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationFailedException(errors);
        }
    }

    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
