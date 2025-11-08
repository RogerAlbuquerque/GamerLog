import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { Spoiler } from "./Spoiler";

describe("Spoiler", () => {
  it("shows ordinary content immediately", () => {
    render(<Spoiler isSpoiler={false}>plain text</Spoiler>);
    expect(screen.getByText("plain text")).toBeInTheDocument();
  });

  it("does not put hidden content in the DOM", () => {
    render(<Spoiler isSpoiler>the butler did it</Spoiler>);
    expect(screen.queryByText("the butler did it")).not.toBeInTheDocument();
    expect(screen.getByText("This contains spoilers.")).toBeInTheDocument();
  });

  it("reveals on request", async () => {
    render(<Spoiler isSpoiler>the butler did it</Spoiler>);
    await userEvent.click(screen.getByRole("button", { name: "Show anyway" }));
    expect(screen.getByText("the butler did it")).toBeInTheDocument();
  });

  it("honours the reveal-by-default preference", () => {
    render(<Spoiler isSpoiler revealByDefault>the butler did it</Spoiler>);
    expect(screen.getByText("the butler did it")).toBeInTheDocument();
  });
});
