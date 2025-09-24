using GameLog.Application.Abstractions;
using GameLog.Application.Common;
using GameLog.Domain.Entities;
using GameLog.Domain.Enums;
using GameLog.Domain.Rules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameLog.Application.Diary;

/// <summary>
/// The gaming diary. Every method takes the acting user id from the authenticated session and only
/// ever touches that user's rows, so ownership cannot be spoofed from a request body.
/// </summary>
public sealed partial class DiaryService(IAppDbContext db, TimeProvider clock, ILogger<DiaryService> logger)
{
    public const int MaxNotesLength = 2000;

    private DateOnly Today => DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

    private sealed record EntryRow(
        Guid Id, Guid GameId, string GameSlug, string GameTitle, string? CoverKey, DiaryStatus Status, int? PlatformId,
        string? Platform, int TotalPlaytimeMinutes, int? RatingHalfStars, bool? Recommended, string? Notes,
        DateTime CreatedAt, DateTime UpdatedAt, List<PlaythroughDto> Playthroughs);

    private IQueryable<EntryRow> Project(IQueryable<GamingDiaryEntry> entries)
    {
        var ratings = db.ReadOnly<Rating>();
        return entries.Select(e => new EntryRow(
            e.Id, e.GameId, e.Game.Slug, e.Game.Title, e.Game.CoverKey, e.Status, e.PlatformId,
            e.Platform != null ? e.Platform.Name : null, e.TotalPlaytimeMinutes,
            ratings.Where(r => r.UserId == e.UserId && r.GameId == e.GameId).Select(r => (int?)r.HalfStars).FirstOrDefault(),
            e.Recommended, e.Notes, e.CreatedAt, e.UpdatedAt,
            e.Playthroughs.OrderBy(p => p.Number)
                .Select(p => new PlaythroughDto(p.Id, p.Number, p.StartedOn, p.CompletedOn, p.PlaytimeMinutes, p.PlatformId,
                    p.Platform != null ? p.Platform.Name : null, p.Notes))
                .ToList()));
    }

    private static DiaryEntryDto ToDto(EntryRow r)
    {
        var latest = r.Playthroughs.Count > 0 ? r.Playthroughs[^1] : null;
        return new DiaryEntryDto(
            r.Id, r.GameId, r.GameSlug, r.GameTitle, MediaUrls.For(r.CoverKey), r.Status, r.PlatformId, r.Platform,
            r.TotalPlaytimeMinutes, r.RatingHalfStars, r.Recommended, r.Notes,
            latest?.StartedOn, latest?.CompletedOn, r.CreatedAt, r.UpdatedAt, r.Playthroughs);
    }

    /// <summary>Diaries are public: anyone can read a user's entries, newest activity first.</summary>
    public async Task<PagedResult<DiaryEntryDto>> ListAsync(
        string username, DiaryStatus? status, int? page, int? pageSize, CancellationToken ct = default)
    {
        var key = UserRules.NormalizeUsername(username ?? "");
        var userId = await db.ReadOnly<User>().Where(u => u.UsernameNormalized == key).Select(u => (Guid?)u.Id).FirstOrDefaultAsync(ct)
                     ?? throw new NotFoundException("User not found.");

        var entries = db.ReadOnly<GamingDiaryEntry>().Where(e => e.UserId == userId);
        if (status is { } s)
        {
            entries = entries.Where(e => e.Status == s);
        }

        var result = await Project(entries.OrderByDescending(e => e.UpdatedAt).ThenBy(e => e.Id)).ToPagedAsync(page, pageSize, ct);
        return new PagedResult<DiaryEntryDto>(result.Items.Select(ToDto).ToList(), result.Page, result.PageSize, result.Total);
    }

    public async Task<DiaryEntryDto> GetAsync(Guid userId, Guid gameId, CancellationToken ct = default)
    {
        var row = await Project(db.ReadOnly<GamingDiaryEntry>().Where(e => e.UserId == userId && e.GameId == gameId))
            .FirstOrDefaultAsync(ct) ?? throw new NotFoundException("This game is not in your diary.");
        return ToDto(row);
    }
}
