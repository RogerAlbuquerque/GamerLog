import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { StarDisplay, StarInput } from "./StarRating";

describe("StarDisplay", () => {
  it("describes the rating for assistive technology", () => {
    render(<StarDisplay value={4.5} />);
    expect(screen.getByRole("img", { name: "Rated 4.5 out of 5" })).toBeInTheDocument();
  });

  it("says when there is no rating", () => {
    render(<StarDisplay value={null} />);
    expect(screen.getByRole("img", { name: "Not rated" })).toBeInTheDocument();
  });

  it("can show the numeric value", () => {
    render(<StarDisplay value={3} showValue />);
    expect(screen.getByText("3.0")).toBeInTheDocument();
  });
});

describe("StarInput", () => {
  const setup = (value: number | null) => {
    const onChange = vi.fn();
    render(<StarInput value={value} onChange={onChange} />);
    return { onChange, slider: screen.getByRole("slider", { name: "Your rating" }) };
  };

  it("exposes the value as accessible text", () => {
    const { slider } = setup(9);
    expect(slider).toHaveAttribute("aria-valuenow", "9");
    expect(slider).toHaveAttribute("aria-valuetext", "4.5 out of 5 stars");
  });

  it("starts at half a star on the first arrow press", () => {
    const { slider, onChange } = setup(null);
    fireEvent.keyDown(slider, { key: "ArrowRight" });
    expect(onChange).toHaveBeenCalledWith(1);
  });

  it("moves by half-stars and stays within 1-10", () => {
    const { slider, onChange } = setup(10);
    fireEvent.keyDown(slider, { key: "ArrowRight" });
    expect(onChange).toHaveBeenLastCalledWith(10);
    fireEvent.keyDown(slider, { key: "ArrowLeft" });
    expect(onChange).toHaveBeenLastCalledWith(9);
  });

  it("jumps with Home/End and clears with Delete", () => {
    const { slider, onChange } = setup(5);
    fireEvent.keyDown(slider, { key: "End" });
    expect(onChange).toHaveBeenLastCalledWith(10);
    fireEvent.keyDown(slider, { key: "Home" });
    expect(onChange).toHaveBeenLastCalledWith(1);
    fireEvent.keyDown(slider, { key: "Delete" });
    expect(onChange).toHaveBeenLastCalledWith(null);
  });

  it("ignores unrelated keys", () => {
    const { slider, onChange } = setup(5);
    fireEvent.keyDown(slider, { key: "a" });
    expect(onChange).not.toHaveBeenCalled();
  });

  it("does nothing when disabled", () => {
    const onChange = vi.fn();
    render(<StarInput value={5} onChange={onChange} disabled />);
    fireEvent.keyDown(screen.getByRole("slider"), { key: "ArrowRight" });
    expect(onChange).not.toHaveBeenCalled();
  });
});
