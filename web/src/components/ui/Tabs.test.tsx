import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { Segmented, TabNav } from "./Tabs";

describe("TabNav", () => {
  it("marks the active tab and shows counts", () => {
    render(<TabNav label="Profile" tabs={[{ href: "/u/a", label: "Activity", active: true }, { href: "/u/a/diary", label: "Diary", active: false, count: 12 }]} />);
    expect(screen.getByRole("link", { name: "Activity" })).toHaveAttribute("aria-current", "page");
    expect(screen.getByRole("link", { name: /Diary/ })).not.toHaveAttribute("aria-current");
    expect(screen.getByText("12")).toBeInTheDocument();
  });
});

describe("Segmented", () => {
  it("reports the pressed option and exposes pressed state", async () => {
    const onChange = vi.fn();
    render(<Segmented label="Sort" value="a" onChange={onChange} options={[{ value: "a", label: "A" }, { value: "b", label: "B" }]} />);
    expect(screen.getByRole("button", { name: "A" })).toHaveAttribute("aria-pressed", "true");
    await userEvent.click(screen.getByRole("button", { name: "B" }));
    expect(onChange).toHaveBeenCalledWith("b");
  });
});
