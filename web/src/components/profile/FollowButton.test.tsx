import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { SessionUser } from "@/lib/api/types";
import { useSession } from "@/stores/session";
import { FollowButton } from "./FollowButton";

const push = vi.fn();
vi.mock("next/navigation", () => ({ useRouter: () => ({ push }), usePathname: () => "/u/bruno" }));
const api = vi.hoisted(() => ({ follow: vi.fn(), unfollow: vi.fn() }));
vi.mock("@/lib/api/endpoints", () => ({ usersApi: api }));

const state = { following: false, followers: 3, followingCount: 1 };
const me = { id: "1", username: "Ana" } as unknown as SessionUser;

describe("FollowButton", () => {
  beforeEach(() => vi.clearAllMocks());

  it("is hidden on your own profile (case-insensitive)", () => {
    useSession.setState({ user: me, status: "ready" });
    const { container } = render(<FollowButton username="ana" state={state} onChange={vi.fn()} />);
    expect(container).toBeEmptyDOMElement();
  });

  it("sends visitors to sign in", async () => {
    useSession.setState({ user: null, status: "ready" });
    render(<FollowButton username="bruno" state={state} onChange={vi.fn()} />);
    await userEvent.click(screen.getByRole("button", { name: "Follow" }));
    expect(push).toHaveBeenCalledWith("/login?next=%2Fu%2Fbruno");
  });

  it("follows and reports the new state", async () => {
    useSession.setState({ user: me, status: "ready" });
    const next = { ...state, following: true, followers: 4 };
    api.follow.mockResolvedValue(next);
    const onChange = vi.fn();
    render(<FollowButton username="bruno" state={state} onChange={onChange} />);
    await userEvent.click(screen.getByRole("button", { name: "Follow" }));
    await waitFor(() => expect(onChange).toHaveBeenCalledWith(next));
    expect(api.follow).toHaveBeenCalledWith("bruno");
  });

  it("unfollows when already following", async () => {
    useSession.setState({ user: me, status: "ready" });
    api.unfollow.mockResolvedValue({ ...state, following: false });
    render(<FollowButton username="bruno" state={{ ...state, following: true }} onChange={vi.fn()} />);
    const button = screen.getByRole("button", { name: "Following" });
    expect(button).toHaveAttribute("aria-pressed", "true");
    await userEvent.click(button);
    await waitFor(() => expect(api.unfollow).toHaveBeenCalledWith("bruno"));
  });
});
