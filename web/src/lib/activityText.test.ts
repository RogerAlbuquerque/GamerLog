import { describe, expect, it } from "vitest";
import type { Activity } from "@/lib/api/types";
import { describeActivity } from "./activityText";

const base: Activity = {
  id: "1", type: "GameCompleted", actor: { id: "u", username: "roger", displayName: "Roger", avatarUrl: null },
  game: { id: "g", slug: "hades", title: "Hades", coverUrl: null }, reviewId: null, reviewTitle: null, collectionId: null,
  collectionName: null, ratingHalfStars: null, createdAt: "2025-11-01T00:00:00Z",
};

describe("describeActivity", () => {
  it("describes completions", () => {
    expect(describeActivity(base)).toEqual({ verb: "completed", href: "/games/hades", target: "Hades" });
  });

  it("describes diary additions", () => {
    expect(describeActivity({ ...base, type: "GameAddedToDiary" }).verb).toBe("added to their diary");
  });

  it("includes the star value when rating", () => {
    expect(describeActivity({ ...base, type: "GameRated", ratingHalfStars: 9 }).verb).toBe("rated 4.5★");
    expect(describeActivity({ ...base, type: "GameRated", ratingHalfStars: null }).verb).toBe("rated");
  });

  it("links reviews to the review page", () => {
    expect(describeActivity({ ...base, type: "ReviewPublished", reviewId: "r1" })).toEqual({ verb: "reviewed", href: "/reviews/r1", target: "Hades" });
  });

  it("links collections to the collection page", () => {
    const line = describeActivity({ ...base, type: "CollectionCreated", game: null, collectionId: "c1", collectionName: "Cozy games" });
    expect(line).toEqual({ verb: "created the collection", href: "/collections/c1", target: "Cozy games" });
  });
});
