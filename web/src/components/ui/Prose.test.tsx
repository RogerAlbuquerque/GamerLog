import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { Prose } from "./Prose";

describe("Prose", () => {
  it("renders markup as literal text instead of HTML", () => {
    const { container } = render(<Prose text={'<img src=x onerror="alert(1)"><script>alert(2)</script>'} />);
    expect(container.querySelector("img")).toBeNull();
    expect(container.querySelector("script")).toBeNull();
    expect(screen.getByText(/<script>alert\(2\)<\/script>/)).toBeInTheDocument();
  });

  it("splits blank lines into paragraphs", () => {
    const { container } = render(<Prose text={"one\n\ntwo\n\n\nthree"} />);
    expect(container.querySelectorAll("p")).toHaveLength(3);
  });

  it("keeps single line breaks", () => {
    const { container } = render(<Prose text={"line a\nline b"} />);
    expect(container.querySelector("p")).toHaveClass("whitespace-pre-line");
  });
});
