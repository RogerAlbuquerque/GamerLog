import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { Comment, SessionUser } from "@/lib/api/types";
import { useSession } from "@/stores/session";
import { CommentThread } from "./CommentThread";

vi.mock("next/navigation", () => ({ useRouter: () => ({ push: vi.fn() }), usePathname: () => "/games/x" }));

const api = vi.hoisted(() => ({
  forGame: vi.fn(),
  forReview: vi.fn(),
  replies: vi.fn(),
  createOnGame: vi.fn(),
  createOnReview: vi.fn(),
  edit: vi.fn(),
  remove: vi.fn(),
  like: vi.fn(),
  unlike: vi.fn(),
}));
vi.mock("@/lib/api/endpoints", () => ({ commentsApi: api }));

const author = { id: "u2", username: "bruno", displayName: "Bruno", avatarUrl: null };
const base: Comment = {
  id: "c1", parentId: null, author, body: "Loved the ending", isSpoiler: false, isDeleted: false,
  likeCount: 0, replyCount: 0, likedByMe: false, createdAt: "2025-10-01T10:00:00Z", editedAt: null,
};
const page = (items: Comment[]) => ({ items, page: 1, pageSize: 15, total: items.length });
const me = { id: "u1", username: "ana", displayName: "Ana", role: "User", showSpoilersByDefault: false } as unknown as SessionUser;

describe("CommentThread", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useSession.setState({ user: null, status: "ready" });
  });

  it("shows an empty state", async () => {
    api.forGame.mockResolvedValue(page([]));
    render(<CommentThread target={{ kind: "game", id: "g1" }} />);
    expect(await screen.findByText("No comments yet")).toBeInTheDocument();
  });

  it("asks visitors to sign in instead of showing the composer", async () => {
    api.forGame.mockResolvedValue(page([base]));
    render(<CommentThread target={{ kind: "game", id: "g1" }} />);
    expect(await screen.findByText("Loved the ending")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Sign in" })).toBeInTheDocument();
  });

  it("renders deleted comments as placeholders and keeps their replies visible", async () => {
    api.forGame.mockResolvedValue(page([{ ...base, isDeleted: true, author: null, body: "", replyCount: 2 }]));
    render(<CommentThread target={{ kind: "game", id: "g1" }} />);
    expect(await screen.findByText(/\[deleted comment\]/)).toHaveTextContent("2 replies");
  });

  it("offers edit and delete only on the viewer's own comments", async () => {
    useSession.setState({ user: me });
    api.forGame.mockResolvedValue(page([base, { ...base, id: "c2", author: { ...author, id: "u1", username: "ana", displayName: "Ana" }, body: "mine" }]));
    render(<CommentThread target={{ kind: "game", id: "g1" }} />);
    await screen.findByText("mine");
    expect(screen.getAllByRole("button", { name: "Delete" })).toHaveLength(1);
    expect(screen.getAllByRole("button", { name: "Edit" })).toHaveLength(1);
  });

  it("posts a new comment and reloads the list", async () => {
    useSession.setState({ user: me });
    api.forReview.mockResolvedValue(page([]));
    api.createOnReview.mockResolvedValue(base);
    render(<CommentThread target={{ kind: "review", id: "r1" }} />);
    await screen.findByText("No comments yet");
    await userEvent.type(screen.getByRole("textbox"), "Great take");
    await userEvent.click(screen.getByRole("button", { name: "Post comment" }));
    await waitFor(() => expect(api.createOnReview).toHaveBeenCalledWith("r1", { body: "Great take", isSpoiler: false }));
    await waitFor(() => expect(api.forReview.mock.calls.length).toBeGreaterThan(1));
  });

  it("renders comment markup as text", async () => {
    api.forGame.mockResolvedValue(page([{ ...base, body: "<script>alert(1)</script>" }]));
    render(<CommentThread target={{ kind: "game", id: "g1" }} />);
    expect(await screen.findByText("<script>alert(1)</script>")).toBeInTheDocument();
    expect(document.querySelector("script")).toBeNull();
  });
});
