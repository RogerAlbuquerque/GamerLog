using GameLog.Application.Abstractions;
using GameLog.Application.Activity;
using GameLog.Application.Common;
using GameLog.Domain.Entities;
using GameLog.Domain.Enums;
using GameLog.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace GameLog.Application.Ratings;

/// <summary>
/// Independent game ratings: at most one per user and game (a unique index guarantees it), stored as
/// half-stars. The game's count and sum are recomputed from the Ratings table in the same transaction,
/// so the community average is always consistent and reads stay O(1).
/// </summary>
public sealed class RatingService(IAppDbContext db, TimeProvider clock, ActivityService activity)
{
    public async Task<RatingDto> SetAsync(Guid userId, Guid gameId, int halfStars, CancellationToken ct = default)
    {
        await ApplyAsync(userId, gameId, halfStars, ct);
        await PersistAsync(gameId, ct);
        return await GetMineAsync(userId, gameId, ct);
    }

    public async Task RemoveAsync(Guid userId, Guid gameId, CancellationToken ct = default)
    {
        var rating = await db.Query<Rating>().FirstOrDefaultAsync(r => r.UserId == userId && r.GameId == gameId, ct)
                     ?? throw new NotFoundException("You have not rated this game.");
        db.Set<Rating>().Remove(rating);
        await activity.WithdrawAsync(userId, ActivityType.GameRated, gameId, ct: ct);
        await PersistAsync(gameId, ct);
    }

    public async Task<RatingDto> GetMineAsync(Guid userId, Guid gameId, CancellationToken ct = default)
    {
        var r = await db.ReadOnly<Rating>().FirstOrDefaultAsync(x => x.UserId == userId && x.GameId == gameId, ct)
                ?? throw new NotFoundException("You have not rated this game.");
        return new RatingDto(r.GameId, r.HalfStars, RatingRules.ToStars(r.HalfStars), r.UpdatedAt);
    }

    public async Task<RatingSummaryDto> SummaryAsync(Guid gameId, CancellationToken ct = default)
    {
        if (!await db.ReadOnly<Game>().AnyAsync(g => g.Id == gameId, ct))
        {
            throw new NotFoundException("Game not found.");
        }

        var buckets = await db.ReadOnly<Rating>()
            .Where(r => r.GameId == gameId)
            .GroupBy(r => r.HalfStars)
            .Select(g => new { HalfStars = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var distribution = new int[RatingRules.MaxHalfStars];
        foreach (var b in buckets)
        {
            distribution[b.HalfStars - 1] = b.Count;
        }

        // Derived from the same rows as the distribution, so the numbers can never disagree.
        var count = distribution.Sum();
        var sum = distribution.Select((n, i) => n * (i + 1)).Sum();
        double? average = count == 0 ? null : Math.Round(sum / 2.0 / count, 2);
        return new RatingSummaryDto(gameId, average, count, distribution);
    }

    /// <summary>Validates and records the rating on tracked entities without saving (reviews reuse this).</summary>
    public async Task ApplyAsync(Guid userId, Guid gameId, int halfStars, CancellationToken ct = default)
    {
        if (!RatingRules.IsValid(halfStars))
        {
            throw new ValidationFailedException("halfStars", "A rating must be between 0.5 and 5 stars in half-star steps.");
        }

        if (!await db.ReadOnly<Game>().AnyAsync(g => g.Id == gameId, ct))
        {
            throw new NotFoundException("Game not found.");
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var rating = await db.Query<Rating>().FirstOrDefaultAsync(r => r.UserId == userId && r.GameId == gameId, ct);
        if (rating is null)
        {
            db.Set<Rating>().Add(new Rating { UserId = userId, GameId = gameId, HalfStars = halfStars, CreatedAt = now, UpdatedAt = now });
        }
        else
        {
            rating.HalfStars = halfStars;
            rating.UpdatedAt = now;
        }

        await activity.StageAsync(userId, ActivityType.GameRated, gameId, value: halfStars, ct: ct);
    }

    /// <summary>Saves pending changes and refreshes the game's rating counters in one transaction.</summary>
    public async Task PersistAsync(Guid gameId, CancellationToken ct = default)
    {
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            await db.SaveChangesAsync(ct);
            await GameCounters.RecalculateRatingsAsync(db, gameId, ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            // A concurrent first rating by the same user hit the unique (user, game) index.
            throw new ConflictException("rating_conflict", "Your rating was changed at the same time. Please try again.");
        }
    }
}
