import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { SessionUser } from "@/lib/api/types";
import { useSession } from "@/stores/session";
import { Header } from "./Header";

const push = vi.fn();
vi.mock("next/navigation", () => ({ useRouter: () => ({ push }), usePathname: () => "/discover" }));
vi.mock("@/hooks/useUnreadCount", () => ({ useUnreadCount: () => ({ count: 3 }) }));
const user = (role: string) => ({ id: "1", username: "ana", displayName: "Ana", avatarUrl: null, role }) as unknown as SessionUser;

describe("Header", () => {
  beforeEach(() => push.mockClear());

  it("offers sign in and registration to visitors", () => {
    useSession.setState({ user: null, status: "ready" });
    render(<Header />);
    expect(screen.getByRole("link", { name: "Sign in" })).toHaveAttribute("href", "/login");
    expect(screen.getByRole("link", { name: "Join free" })).toHaveAttribute("href", "/register");
    expect(screen.queryByRole("link", { name: "Feed" })).not.toBeInTheDocument();
  });

  it("shows member links and the unread badge when signed in", () => {
    useSession.setState({ user: user("User"), status: "ready" });
    render(<Header />);
    expect(screen.getByRole("link", { name: "Feed" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Notifications, 3 unread" })).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Import games" })).not.toBeInTheDocument();
  });

  it("shows the import link to admins in both menus", async () => {
    useSession.setState({ user: user("Admin"), status: "ready" });
    render(<Header />);
    expect(screen.getAllByRole("link", { name: "Import games" })).toHaveLength(1);
    await userEvent.click(screen.getByRole("button", { name: "Open menu" }));
    expect(screen.getAllByRole("link", { name: "Import games" })).toHaveLength(2);
  });

  it("searches from the global box", async () => {
    useSession.setState({ user: null, status: "ready" });
    render(<Header />);
    await userEvent.type(screen.getAllByRole("textbox", { name: "Search games" })[0], "zelda{Enter}");
    expect(push).toHaveBeenCalledWith("/search?q=zelda");
  });
});
