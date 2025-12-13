import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { SessionUser } from "@/lib/api/types";
import { useSession } from "@/stores/session";
import { useToasts } from "@/stores/toast";
import { LikeButton } from "./LikeButton";

const push = vi.fn();
vi.mock("next/navigation", () => ({ useRouter: () => ({ push }), usePathname: () => "/reviews/r1" }));

const signedIn = { id: "u1" } as unknown as SessionUser;

describe("LikeButton", () => {
  beforeEach(() => {
    push.mockClear();
    useToasts.setState({ toasts: [] });
  });

  it("sends visitors to sign in and brings them back", async () => {
    useSession.setState({ user: null, status: "ready" });
    const onToggle = vi.fn();
    render(<LikeButton liked={false} count={2} label="this review" onToggle={onToggle} />);
    await userEvent.click(screen.getByRole("button", { name: "Like this review" }));
    expect(push).toHaveBeenCalledWith("/login?next=%2Freviews%2Fr1");
    expect(onToggle).not.toHaveBeenCalled();
  });

  it("updates optimistically and settles on the server answer", async () => {
    useSession.setState({ user: signedIn, status: "ready" });
    const onToggle = vi.fn().mockResolvedValue({ liked: true, likeCount: 5 });
    render(<LikeButton liked={false} count={2} label="this review" onToggle={onToggle} />);
    await userEvent.click(screen.getByRole("button", { name: "Like this review" }));
    expect(onToggle).toHaveBeenCalledWith(true);
    await waitFor(() => expect(screen.getByRole("button", { name: "Unlike this review" })).toHaveTextContent("5"));
  });

  it("rolls back and reports failures", async () => {
    useSession.setState({ user: signedIn, status: "ready" });
    const onToggle = vi.fn().mockRejectedValue(new Error("offline"));
    render(<LikeButton liked={false} count={2} label="this review" onToggle={onToggle} />);
    await userEvent.click(screen.getByRole("button", { name: "Like this review" }));
    await waitFor(() => expect(useToasts.getState().toasts).toHaveLength(1));
    expect(screen.getByRole("button", { name: "Like this review" })).toHaveTextContent("2");
  });
});
