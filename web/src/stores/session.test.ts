import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "@/lib/api/client";
import type { SessionUser } from "@/lib/api/types";

vi.mock("@/lib/api/endpoints", () => ({
  authApi: { me: vi.fn(), login: vi.fn(), register: vi.fn(), logout: vi.fn() },
}));

import { authApi } from "@/lib/api/endpoints";
import { useSession } from "./session";

const user: SessionUser = { id: "1", username: "roger", displayName: "Roger", avatarUrl: null, role: "User", showSpoilersByDefault: false };
const api = vi.mocked(authApi);

beforeEach(() => {
  vi.clearAllMocks();
  useSession.setState({ user: null, status: "idle" });
});

describe("session store", () => {
  it("loads the signed-in user", async () => {
    api.me.mockResolvedValue(user);
    await useSession.getState().load();
    expect(useSession.getState()).toMatchObject({ user, status: "ready" });
  });

  it("treats 401 as signed out", async () => {
    api.me.mockRejectedValue(new ApiError(401, "no"));
    await useSession.getState().load();
    expect(useSession.getState()).toMatchObject({ user: null, status: "ready" });
  });

  it("only loads once", async () => {
    api.me.mockResolvedValue(user);
    await useSession.getState().load();
    await useSession.getState().load();
    expect(api.me).toHaveBeenCalledTimes(1);
  });

  it("stores the user after login", async () => {
    api.login.mockResolvedValue({ user, csrfToken: "t" });
    await useSession.getState().login("roger", "pw");
    expect(useSession.getState().user).toEqual(user);
  });

  it("surfaces login failures and stays signed out", async () => {
    api.login.mockRejectedValue(new ApiError(401, "Invalid username or password."));
    await expect(useSession.getState().login("roger", "bad")).rejects.toThrow("Invalid");
    expect(useSession.getState().user).toBeNull();
  });

  it("signs out locally even when the request fails", async () => {
    useSession.setState({ user, status: "ready" });
    api.logout.mockRejectedValue(new ApiError(500, "boom"));
    await expect(useSession.getState().logout()).rejects.toThrow();
    expect(useSession.getState().user).toBeNull();
  });

  it("clears the user when any request reports an expired session", async () => {
    useSession.setState({ user, status: "ready" });
    const { request } = await import("@/lib/api/client");
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(null, { status: 401 })));
    await expect(request("GET", "/auth/me")).rejects.toBeInstanceOf(ApiError);
    expect(useSession.getState().user).toBeNull();
    vi.unstubAllGlobals();
  });
});
