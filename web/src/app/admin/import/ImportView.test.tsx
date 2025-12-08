import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { SessionUser } from "@/lib/api/types";
import { useSession } from "@/stores/session";
import { ImportView } from "./ImportView";

vi.mock("next/navigation", () => ({ useRouter: () => ({ replace: vi.fn(), push: vi.fn() }), usePathname: () => "/admin/import" }));
const api = vi.hoisted(() => ({ searchProvider: vi.fn(), importGame: vi.fn() }));
vi.mock("@/lib/api/endpoints", () => ({ adminApi: api }));

const external = {
  provider: "igdb", externalId: "1", title: "Hades", summary: null, releaseDate: "2020-09-17", developer: "Supergiant",
  publisher: null, coverUrl: null, genres: ["Roguelike"], platforms: ["PC"],
};

describe("ImportView", () => {
  beforeEach(() => vi.clearAllMocks());

  it("turns non-admins away without calling the API", () => {
    useSession.setState({ user: { id: "1", role: "User" } as unknown as SessionUser, status: "ready" });
    render(<ImportView />);
    expect(screen.getByText("Administrators only")).toBeInTheDocument();
    expect(api.searchProvider).not.toHaveBeenCalled();
  });

  it("searches and imports for admins", async () => {
    useSession.setState({ user: { id: "1", role: "Admin" } as unknown as SessionUser, status: "ready" });
    api.searchProvider.mockResolvedValue([external]);
    api.importGame.mockResolvedValue({ id: "g", slug: "hades", title: "Hades", created: true });
    render(<ImportView />);
    await userEvent.type(screen.getByLabelText("Game title"), "hades");
    await userEvent.click(screen.getByRole("button", { name: "Search" }));
    expect(await screen.findByText(/Supergiant/)).toBeInTheDocument();
    expect(api.searchProvider).toHaveBeenCalledWith("hades");
    await userEvent.click(screen.getByRole("button", { name: "Import" }));
    await waitFor(() => expect(api.importGame).toHaveBeenCalledWith("1"));
    expect(await screen.findByRole("link", { name: /View in catalog/ })).toHaveAttribute("href", "/games/hades");
  });
});
