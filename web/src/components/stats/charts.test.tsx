import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { BarList, Donut, MonthlyChart, StatTile } from "./charts";

describe("charts", () => {
  it("renders a stat tile", () => {
    render(<StatTile label="Completed" value={12} hint="this year" />);
    expect(screen.getByText("Completed")).toBeInTheDocument();
    expect(screen.getByText("12")).toBeInTheDocument();
  });

  it("gives bar lists a table alternative for screen readers", () => {
    render(<BarList caption="Top genres" data={[{ label: "RPG", value: 5 }, { label: "Puzzle", value: 2 }]} />);
    expect(screen.getByRole("table", { name: "Top genres", hidden: true })).toBeInTheDocument();
    expect(screen.getByRole("rowheader", { name: "RPG", hidden: true })).toBeInTheDocument();
  });

  it("explains an empty bar list", () => {
    render(<BarList caption="x" data={[]} />);
    expect(screen.getByText("Not enough data yet.")).toBeInTheDocument();
  });

  it("describes the monthly chart in words", () => {
    render(<MonthlyChart values={[1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3]} />);
    expect(screen.getByRole("img")).toHaveAttribute("aria-label", expect.stringContaining("Dec 3"));
  });

  it("handles an all-zero donut", () => {
    render(<Donut total={0} centerLabel="games" slices={[{ label: "Completed", value: 0, color: "#fff" }]} />);
    expect(screen.getByRole("img")).toHaveAttribute("aria-label", "Completed: 0");
  });
});
