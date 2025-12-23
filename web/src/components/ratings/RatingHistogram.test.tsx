import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { RatingHistogram } from "./RatingHistogram";

const dist = [0, 0, 0, 0, 1, 0, 2, 3, 0, 4];

describe("RatingHistogram", () => {
  it("shows the average, the count and an accessible distribution", () => {
    render(<RatingHistogram summary={{ gameId: "g", average: 4.3, count: 10, distribution: dist }} />);
    expect(screen.getByText("4.3")).toBeInTheDocument();
    expect(screen.getByText("10 ratings")).toBeInTheDocument();
    const chart = screen.getByRole("img", { name: /Rating distribution/ });
    expect(chart).toHaveAttribute("aria-label", expect.stringContaining("4 at 5 stars"));
    expect(chart).toHaveAttribute("aria-label", expect.stringContaining("1 at 2.5 stars"));
  });

  it("handles a game nobody has rated", () => {
    render(<RatingHistogram summary={{ gameId: "g", average: null, count: 0, distribution: Array(10).fill(0) }} />);
    expect(screen.getByText("—")).toBeInTheDocument();
    expect(screen.getByText("No ratings yet")).toBeInTheDocument();
  });
});
