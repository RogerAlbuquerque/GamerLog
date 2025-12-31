import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { Logo } from "./Logo";

describe("Logo", () => {
  it("links home with an accessible name", () => {
    render(<Logo />);
    expect(screen.getByRole("link", { name: "GameLog home" })).toHaveAttribute("href", "/");
  });
});
