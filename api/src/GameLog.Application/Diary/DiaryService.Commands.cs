using GameLog.Application.Common;
using GameLog.Domain.Entities;
using GameLog.Domain.Enums;
using GameLog.Domain.Rules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameLog.Application.Diary;

public sealed partial class DiaryService
{
    /// <summary>
    /// Creates or updates the caller's entry. The dates and playtime describe the latest playthrough.
    /// If the latest one is already completed and the game is picked up again, a new playthrough is
    /// started instead of overwriting history.
    /// </summary>
    public async Task<DiaryEntryDto> UpsertAsync(Guid userId, Guid gameId, UpsertDiaryRequest r, CancellationToken ct = default)
    {
        if (!await db.ReadOnly<Game>().AnyAsync(g => g.Id == gameId, ct))
        {
            throw new NotFoundException("Game not found.");
        }

        var entry = await db.Query<GamingDiaryEntry>().Include(e => e.Playthroughs)
            .FirstOrDefaultAsync(e => e.UserId == userId && e.GameId == gameId, ct);
        var latest = entry?.Playthroughs.MaxBy(p => p.Number);
        var errors = new Dictionary<string, string[]>();

        if (entry is not null && !DiaryRules.IsTransitionAllowed(entry.Status, r.Status))
        {
            errors["status"] = ["A game you have played cannot go back to Want to Play. Remove it from your diary instead."];
        }

        var startsNewPlaythrough = r.Status != DiaryStatus.WantToPlay && latest is { CompletedOn: not null } && r.Status != DiaryStatus.Completed;
        var today = Today;
        var completedOn = r.Status == DiaryStatus.Completed
            ? r.CompletedOn ?? (startsNewPlaythrough ? null : latest?.CompletedOn) ?? today
            : r.CompletedOn;

        foreach (var v in DiaryRules.Validate(r.Status, r.StartedOn, completedOn, r.PlaytimeMinutes ?? 0, today))
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

        var now = clock.GetUtcNow().UtcDateTime;
        if (entry is null)
        {
            entry = new GamingDiaryEntry { UserId = userId, GameId = gameId, CreatedAt = now };
            db.Set<GamingDiaryEntry>().Add(entry);
        }

        entry.Status = r.Status;
        entry.PlatformId = r.PlatformId;
        entry.Recommended = r.Recommended;
        entry.Notes = string.IsNullOrWhiteSpace(r.Notes) ? null : r.Notes.Trim();
        entry.UpdatedAt = now;

        if (r.Status != DiaryStatus.WantToPlay)
        {
            var isNew = latest is null || startsNewPlaythrough;
            var target = isNew ? new Playthrough { DiaryEntryId = entry.Id, Number = (latest?.Number ?? 0) + 1 } : latest!;
            target.StartedOn = r.StartedOn;
            target.CompletedOn = completedOn;
            target.PlaytimeMinutes = r.PlaytimeMinutes ?? (isNew ? 0 : target.PlaytimeMinutes);
            target.PlatformId = r.PlatformId;
            if (isNew)
            {
                entry.Playthroughs.Add(target);
                db.Set<Playthrough>().Add(target);
            }
        }

        entry.TotalPlaytimeMinutes = TotalPlaytime(entry);
        await PersistAsync(gameId, ct);
        return await GetAsync(userId, gameId, ct);
    }

    /// <summary>Removes the entry and all its playthroughs. The user's rating and review are separate and stay.</summary>
    public async Task DeleteAsync(Guid userId, Guid gameId, CancellationToken ct = default)
    {
        var entry = await db.Query<GamingDiaryEntry>().FirstOrDefaultAsync(e => e.UserId == userId && e.GameId == gameId, ct)
                    ?? throw new NotFoundException("This game is not in your diary.");
        db.Set<GamingDiaryEntry>().Remove(entry);
        await PersistAsync(gameId, ct);
    }

    private static int TotalPlaytime(GamingDiaryEntry entry) =>
        (int)Math.Min(entry.Playthroughs.Sum(p => (long)p.PlaytimeMinutes), DiaryRules.MaxPlaytimeMinutes);

    /// <summary>Saves and refreshes the game's completed counter in one transaction.</summary>
    private async Task PersistAsync(Guid gameId, CancellationToken ct)
    {
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            await db.SaveChangesAsync(ct);
            await GameCounters.RecalculateCompletedAsync(db, gameId, ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            // Two simultaneous first saves hit the unique (user, game) index.
            logger.LogWarning(ex, "Diary write conflicted with a concurrent write");
            throw new ConflictException("diary_conflict", "Your diary was changed at the same time. Please try again.");
        }
    }
}
