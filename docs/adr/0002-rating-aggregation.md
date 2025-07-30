# ADR 0002 — Rating storage and aggregation

**Status:** accepted

## Decision

- A user has **one rating per game** (`Ratings`, unique on `(UserId, GameId)`).
- Ratings are stored as integer **half-stars (1–10)**, which avoids floating-point
  drift and makes constraints trivial: `CHECK (HalfStars BETWEEN 1 AND 10)`.
- Publishing a review **upserts the rating**; the review never stores its own copy, so
  review score and personal rating cannot diverge.
- `Games.RatingCount` and `Games.RatingSum` are denormalised and changed with
  **atomic SQL updates** (`SET RatingSum = RatingSum + @delta`) inside the same
  transaction as the rating write. Reads of the average are O(1).
- The distribution (histogram) is computed with an indexed `GROUP BY` on
  `(GameId, HalfStars)`.
- `RatingService.RecalculateAsync` rebuilds the aggregates from source rows and is
  used by tests and operational repair.

## Consequences

Averages are consistent under concurrency without locking. The cost is one extra
write per rating change.
