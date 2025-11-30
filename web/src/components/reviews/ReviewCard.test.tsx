import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { Review } from "@/lib/api/types";
import { ReviewCard } from "./ReviewCard";

vi.mock("next/navigation", () => ({ useRouter: () => ({ push: vi.fn() }), usePathname: () => "/" }));

const review: Review = {
  id: "r1", gameId: "g1", gameSlug: "hollow-knight", gameTitle: "Hollow Knight", coverUrl: null,
  author: { id: "u1", username: "ana", displayName: "Ana", avatarUrl: null },
  title: "A bug-sized masterpiece", body: "Tight combat and a haunting map worth getting lost in.",
  experience: null, pros: null, cons: null, favoriteCharacter: null, favoriteMoment: null, difficulty: null,
  playtimeMinutes: 1500, platformId: null, platform: "PC", completedOn: null, containsSpoilers: false,
  ratingHalfStars: 9, likeCount: 3, commentCount: 2, likedByMe: false,
  createdAt: "2025-10-01T10:00:00Z", updatedAt: "2025-10-01T10:00:00Z",
};

describe("ReviewCard", () => {
  it("shows the title as a link to the review and the author", () => {
    render(<ReviewCard review={review} />);
    expect(screen.getByRole("link", { name: "A bug-sized masterpiece" })).toHaveAttribute("href", "/reviews/r1");
    expect(screen.getByRole("link", { name: "Ana" })).toHaveAttribute("href", "/u/ana");
  });

  it("names the game only when asked", () => {
    const { rerender } = render(<ReviewCard review={review} />);
    expect(screen.queryByText("Hollow Knight")).not.toBeInTheDocument();
    rerender(<ReviewCard review={review} showGame />);
    expect(screen.getByRole("link", { name: "Hollow Knight" })).toHaveAttribute("href", "/games/hollow-knight");
  });

  it("hides spoiler reviews until revealed", () => {
    render(<ReviewCard review={{ ...review, containsSpoilers: true }} />);
    expect(screen.queryByText(/haunting map/)).not.toBeInTheDocument();
    expect(screen.getByText("Spoilers")).toBeInTheDocument();
  });

  it("truncates long bodies and offers the full review", () => {
    render(<ReviewCard review={{ ...review, body: "word ".repeat(200) }} />);
    expect(screen.getByRole("link", { name: "Read full review" })).toBeInTheDocument();
  });

  it("renders markup in the body as inert text", () => {
    render(<ReviewCard review={{ ...review, body: "<img src=x onerror=alert(1)> still just text" }} full />);
    expect(document.querySelector("img")).toBeNull();
    expect(screen.getByText(/<img src=x/)).toBeInTheDocument();
  });
});
