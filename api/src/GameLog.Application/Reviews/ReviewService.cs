using GameLog.Application.Abstractions;
using GameLog.Application.Common;
using GameLog.Application.Ratings;
using GameLog.Domain.Entities;
using GameLog.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace GameLog.Application.Reviews;

/// <summary>
/// Reviews: one primary review per user and game (unique index). The score is not part of the review;
/// publishing may also set the rating, which goes through <see cref="RatingService"/> so aggregates stay consistent.
/// </summary>
public sealed class ReviewService(IAppDbContext db, RatingService ratings, TimeProvider clock)
{
    private IQueryable<ReviewDto> Project(IQueryable<Review> reviews, Guid? viewerId)
    {
        var ratingRows = db.ReadOnly<Rating>();
        var likes = db.ReadOnly<ReviewLike>();
        return reviews.Select(r => new ReviewDto(
            r.Id, r.GameId, r.Game.Slug, r.Game.Title, r.Game.CoverKey,
            new ReviewAuthorDto(r.UserId, r.User.Username, r.User.DisplayName, r.User.AvatarKey),
            r.Title, r.Body, r.Experience, r.Pros, r.Cons, r.FavoriteCharacter, r.FavoriteMoment, r.Difficulty,
            r.PlaytimeMinutes, r.PlatformId, r.Platform != null ? r.Platform.Name : null, r.CompletedOn, r.ContainsSpoilers,
            ratingRows.Where(x => x.UserId == r.UserId && x.GameId == r.GameId).Select(x => (int?)x.HalfStars).FirstOrDefault(),
            r.LikeCount, r.CommentCount,
            viewerId != null && likes.Any(l => l.ReviewId == r.Id && l.UserId == viewerId),
            r.CreatedAt, r.UpdatedAt));
    }

    /// <summary>The stored keys are turned into public media URLs after the query runs.</summary>
    private static ReviewDto Finish(ReviewDto d) => d with
    {
        CoverUrl = MediaUrls.For(d.CoverUrl),
        Author = d.Author with { AvatarUrl = MediaUrls.For(d.Author.AvatarUrl) },
    };

    private static IQueryable<Review> Order(IQueryable<Review> q, string? sort) =>
        sort == ReviewSort.Popular
            ? q.OrderByDescending(r => r.LikeCount).ThenByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
            : q.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id);

    private async Task<PagedResult<ReviewDto>> PageAsync(
        IQueryable<Review> q, string? sort, Guid? viewerId, int? page, int? pageSize, CancellationToken ct)
    {
        var result = await Project(Order(q, sort), viewerId).ToPagedAsync(page, pageSize, ct);
        return result with { Items = result.Items.Select(Finish).ToList() };
    }

    public Task<PagedResult<ReviewDto>> ListForGameAsync(
        Guid gameId, string? sort, Guid? viewerId, int? page, int? pageSize, CancellationToken ct = default) =>
        PageAsync(db.ReadOnly<Review>().Where(r => r.GameId == gameId), sort, viewerId, page, pageSize, ct);

    public async Task<PagedResult<ReviewDto>> ListForUserAsync(
        string username, string? sort, Guid? viewerId, int? page, int? pageSize, CancellationToken ct = default)
    {
        var normalized = UserRules.NormalizeUsername(username);
        var userId = await db.ReadOnly<User>().Where(u => u.UsernameNormalized == normalized).Select(u => (Guid?)u.Id).FirstOrDefaultAsync(ct)
                     ?? throw new NotFoundException("User not found.");
        return await PageAsync(db.ReadOnly<Review>().Where(r => r.UserId == userId), sort, viewerId, page, pageSize, ct);
    }

    public async Task<ReviewDto> GetAsync(Guid reviewId, Guid? viewerId, CancellationToken ct = default)
    {
        var dto = await Project(db.ReadOnly<Review>().Where(r => r.Id == reviewId), viewerId).FirstOrDefaultAsync(ct)
                  ?? throw new NotFoundException("Review not found.");
        return Finish(dto);
    }

    public async Task<ReviewDto> SaveAsync(Guid userId, Guid gameId, SaveReviewRequest req, CancellationToken ct = default)
    {
        var errors = ReviewRules.Validate(req.Title, req.Body, req.Experience, req.Pros, req.Cons,
            req.FavoriteCharacter, req.FavoriteMoment, req.PlaytimeMinutes);
        if (errors.Count > 0)
        {
            throw ValidationFailedException.From(errors);
        }

        if (!await db.ReadOnly<Game>().AnyAsync(g => g.Id == gameId, ct))
        {
            throw new NotFoundException("Game not found.");
        }

        if (req.PlatformId is { } platformId && !await db.ReadOnly<Platform>().AnyAsync(p => p.Id == platformId, ct))
        {
            throw new ValidationFailedException("platformId", "Unknown platform.");
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var review = await db.Query<Review>().FirstOrDefaultAsync(r => r.UserId == userId && r.GameId == gameId, ct);
        if (review is null)
        {
            review = new Review { UserId = userId, GameId = gameId, CreatedAt = now };
            db.Set<Review>().Add(review);
        }

        review.Title = req.Title.Trim();
        review.Body = req.Body.Trim();
        review.ContainsSpoilers = req.ContainsSpoilers;
        review.Experience = ReviewRules.Clean(req.Experience);
        review.Pros = ReviewRules.Clean(req.Pros);
        review.Cons = ReviewRules.Clean(req.Cons);
        review.FavoriteCharacter = ReviewRules.Clean(req.FavoriteCharacter);
        review.FavoriteMoment = ReviewRules.Clean(req.FavoriteMoment);
        review.Difficulty = req.Difficulty;
        review.PlaytimeMinutes = req.PlaytimeMinutes;
        review.PlatformId = req.PlatformId;
        review.CompletedOn = req.CompletedOn;
        review.UpdatedAt = now;

        if (req.HalfStars is { } halfStars)
        {
            await ratings.ApplyAsync(userId, gameId, halfStars, ct);
        }

        try
        {
            await ratings.PersistAsync(gameId, ct);
        }
        catch (ConflictException)
        {
            throw new ConflictException("review_conflict", "Your review was changed at the same time. Please try again.");
        }

        return await GetAsync(review.Id, userId, ct);
    }

    /// <summary>Authors delete their own review; admins can moderate any review.</summary>
    public async Task DeleteAsync(Guid actorId, bool isAdmin, Guid reviewId, CancellationToken ct = default)
    {
        var review = await db.Query<Review>().FirstOrDefaultAsync(r => r.Id == reviewId, ct)
                     ?? throw new NotFoundException("Review not found.");
        if (review.UserId != actorId && !isAdmin)
        {
            throw new ForbiddenException("Only the author can delete this review.");
        }

        db.Set<Review>().Remove(review);
        await db.SaveChangesAsync(ct);
    }

    public async Task<LikeResponse> LikeAsync(Guid userId, Guid reviewId, CancellationToken ct = default)
    {
        if (!await db.ReadOnly<Review>().AnyAsync(r => r.Id == reviewId, ct))
        {
            throw new NotFoundException("Review not found.");
        }

        if (!await db.ReadOnly<ReviewLike>().AnyAsync(l => l.ReviewId == reviewId && l.UserId == userId, ct))
        {
            db.Set<ReviewLike>().Add(new ReviewLike { ReviewId = reviewId, UserId = userId, CreatedAt = clock.GetUtcNow().UtcDateTime });
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // A concurrent request already liked it: the end state is the same, so this is a no-op.
            }
        }

        return await RecountAsync(reviewId, true, ct);
    }

    public async Task<LikeResponse> UnlikeAsync(Guid userId, Guid reviewId, CancellationToken ct = default)
    {
        var like = await db.Query<ReviewLike>().FirstOrDefaultAsync(l => l.ReviewId == reviewId && l.UserId == userId, ct);
        if (like is not null)
        {
            db.Set<ReviewLike>().Remove(like);
            await db.SaveChangesAsync(ct);
        }
        else if (!await db.ReadOnly<Review>().AnyAsync(r => r.Id == reviewId, ct))
        {
            throw new NotFoundException("Review not found.");
        }

        return await RecountAsync(reviewId, false, ct);
    }

    private async Task<LikeResponse> RecountAsync(Guid reviewId, bool liked, CancellationToken ct)
    {
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""UPDATE "Reviews" SET "LikeCount" = (SELECT COUNT(*) FROM "ReviewLikes" WHERE "ReviewId" = {reviewId}) WHERE "Id" = {reviewId}""", ct);
        var count = await db.ReadOnly<Review>().Where(r => r.Id == reviewId).Select(r => r.LikeCount).FirstAsync(ct);
        return new LikeResponse(liked, count);
    }
}
