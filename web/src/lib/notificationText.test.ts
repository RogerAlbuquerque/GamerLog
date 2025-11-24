import { describe, expect, it } from "vitest";
import type { AppNotification } from "@/lib/api/types";
import { describeNotification } from "./notificationText";

const n = (over: Partial<AppNotification>): AppNotification => ({
  id: "1", type: "NewFollower", actor: { id: "u", username: "bob", displayName: "Bob", avatarUrl: null }, gameId: null, gameSlug: null,
  gameTitle: null, reviewId: null, commentId: null, isRead: false, createdAt: "2025-11-01T00:00:00Z", ...over,
});

describe("describeNotification", () => {
  it("links new followers to their profile", () => {
    expect(describeNotification(n({}))).toEqual({ text: "started following you", href: "/u/bob" });
  });

  it("links review likes and comments to the review", () => {
    expect(describeNotification(n({ type: "ReviewLiked", reviewId: "r1", gameTitle: "Hades" }))).toEqual({ text: "liked your review of “Hades”", href: "/reviews/r1" });
    expect(describeNotification(n({ type: "ReviewComment", reviewId: "r1" })).href).toBe("/reviews/r1");
  });

  it("links game-level comment notifications to the game", () => {
    expect(describeNotification(n({ type: "CommentReply", gameSlug: "hades", gameTitle: "Hades" }))).toEqual({ text: "replied to your comment on “Hades”", href: "/games/hades" });
  });

  it("falls back to the feed when the target is unknown", () => {
    expect(describeNotification(n({ type: "CommentLiked" })).href).toBe("/feed");
  });
});
