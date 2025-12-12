import { act, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it } from "vitest";
import { toast, useToasts } from "@/stores/toast";
import { Toaster } from "./Toaster";

describe("Toaster", () => {
  afterEach(() => useToasts.setState({ toasts: [] }));

  it("announces errors as alerts and others as status", () => {
    render(<Toaster />);
    act(() => {
      toast.error("Nope");
      toast.success("Saved");
    });
    expect(screen.getByRole("alert")).toHaveTextContent("Nope");
    expect(screen.getByRole("status")).toHaveTextContent("Saved");
  });

  it("can be dismissed", async () => {
    render(<Toaster />);
    act(() => void toast.info("Hello"));
    await userEvent.click(screen.getByRole("button", { name: "Dismiss notification" }));
    expect(screen.queryByText("Hello")).not.toBeInTheDocument();
  });
});
