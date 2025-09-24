using GameLog.Application.Abstractions;
using GameLog.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GameLog.Application.Common;

/// <summary>
/// Denormalised per-game counters are recomputed from source rows with a single UPDATE instead of
/// incremented in memory, so concurrent writers cannot make them drift and any drift self-heals.
/// </summary>
public static class GameCounters
{
    public static Task<int> RecalculateCompletedAsync(IAppDbContext db, Guid gameId, CancellationToken ct) =>
        db.Database.ExecuteSqlInterpolatedAsync(
            $"""
             UPDATE "Games" SET "CompletedCount" =
               (SELECT COUNT(*) FROM "GamingDiaryEntries" WHERE "GameId" = {gameId} AND "Status" = {nameof(DiaryStatus.Completed)})
             WHERE "Id" = {gameId}
             """, ct);

    public static Task<int> RecalculateRatingsAsync(IAppDbContext db, Guid gameId, CancellationToken ct) =>
        db.Database.ExecuteSqlInterpolatedAsync(
            $"""
             UPDATE "Games" SET
               "RatingCount" = (SELECT COUNT(*) FROM "Ratings" WHERE "GameId" = {gameId}),
               "RatingSum" = (SELECT COALESCE(SUM("HalfStars"), 0) FROM "Ratings" WHERE "GameId" = {gameId})
             WHERE "Id" = {gameId}
             """, ct);
}
