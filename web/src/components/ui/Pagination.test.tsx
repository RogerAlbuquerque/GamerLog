import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { Pagination } from "./Pagination";

describe("Pagination", () => {
  it("renders nothing for a single page", () => {
    const { container } = render(<Pagination page={1} pageSize={20} total={20} onPage={() => {}} />);
    expect(container).toBeEmptyDOMElement();
  });

  it("shows the position and disables previous on the first page", () => {
    render(<Pagination page={1} pageSize={20} total={45} onPage={() => {}} />);
    expect(screen.getByText("Page 1 of 3")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /previous/i })).toBeDisabled();
  });

  it("navigates and disables next on the last page", () => {
    const onPage = vi.fn();
    render(<Pagination page={3} pageSize={20} total={45} onPage={onPage} />);
    expect(screen.getByRole("button", { name: /next/i })).toBeDisabled();
    fireEvent.click(screen.getByRole("button", { name: /previous/i }));
    expect(onPage).toHaveBeenCalledWith(2);
  });
});
