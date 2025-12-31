import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import type { SessionUser } from "@/lib/api/types";
import { useSession } from "@/stores/session";
import { Footer } from "./Footer";

describe("Footer", () => {
  it("invites visitors to create an account", () => {
    useSession.setState({ user: null, status: "ready" });
    render(<Footer />);
    expect(screen.getByRole("link", { name: "Create an account" })).toHaveAttribute("href", "/register");
  });

  it("does not nag members", () => {
    useSession.setState({ user: { id: "1" } as unknown as SessionUser, status: "ready" });
    render(<Footer />);
    expect(screen.queryByRole("link", { name: "Create an account" })).not.toBeInTheDocument();
    expect(screen.getByRole("navigation", { name: "Footer" })).toBeInTheDocument();
  });
});
