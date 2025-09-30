namespace GameLog.Application.Ratings;

/// <param name="HalfStars">1-10, i.e. 0.5 to 5 stars in half-star steps.</param>
public sealed record SetRatingRequest(int HalfStars);

public sealed record RatingDto(Guid GameId, int HalfStars, double Stars, DateTime UpdatedAt);

/// <param name="Average">Average in stars (0.5-5), rounded to two decimals; null when nobody has rated yet.</param>
/// <param name="Distribution">Ten buckets: index 0 holds the number of 0.5-star ratings, index 9 the number of 5-star ratings.</param>
public sealed record RatingSummaryDto(Guid GameId, double? Average, int Count, IReadOnlyList<int> Distribution);
