import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import type { GameSummary } from "@/lib/api/types";
import { GameCard, GameGrid } from "./GameCard";

const game: GameSummary = {
  id: "g1", slug: "celeste", title: "Celeste", coverUrl: null, releaseDate: "2018-01-25",
  averageRating: 4.5, ratingCount: 10, genres: ["Platformer", "Indie"], platforms: ["PC"],
};

describe("GameCard", () => {
  it("links to the game page and shows year, genre and rating", () => {
    render(<GameCard game={game} />);
    expect(screen.getByRole("link")).toHaveAttribute("href", "/games/celeste");
    expect(screen.getByText("2018 · Platformer")).toBeInTheDocument();
    expect(screen.getByText("★ 4.5")).toBeInTheDocument();
  });

  it("shows no stray separator when the release year is unknown", () => {
    render(<GameCard game={{ ...game, releaseDate: null }} />);
    expect(screen.getByText("Platformer")).toBeInTheDocument();
  });

  it("omits the rating badge for unrated games", () => {
    render(<GameCard game={{ ...game, averageRating: null, ratingCount: 0 }} />);
    expect(screen.queryByText(/★ \d/)).not.toBeInTheDocument();
  });

  it("shows an optional note", () => {
    render(<GameCard game={game} note="Because you liked Hades" />);
    expect(screen.getByText("Because you liked Hades")).toBeInTheDocument();
  });

  it("renders a grid as a list", () => {
    render(<GameGrid games={[game, { ...game, id: "g2", slug: "hades", title: "Hades" }]} />);
    expect(screen.getAllByRole("listitem")).toHaveLength(2);
  });
});
