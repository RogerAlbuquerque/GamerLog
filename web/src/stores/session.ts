import { create } from "zustand";
import { ApiError, onUnauthorized, setCsrfToken } from "@/lib/api/client";
import { authApi } from "@/lib/api/endpoints";
import type { SessionUser } from "@/lib/api/types";

interface SessionState {
  user: SessionUser | null;
  /** "idle" until the first /auth/me answer arrives, so the UI can avoid flashing a signed-out state. */
  status: "idle" | "loading" | "ready";
  load: () => Promise<void>;
  login: (login: string, password: string) => Promise<SessionUser>;
  register: (input: { username: string; email: string; password: string; displayName?: string }) => Promise<SessionUser>;
  logout: () => Promise<void>;
  setUser: (user: SessionUser) => void;
  clear: () => void;
}

export const useSession = create<SessionState>((set, get) => ({
  user: null,
  status: "idle",

  async load() {
    if (get().status !== "idle") return;
    set({ status: "loading" });
    try {
      set({ user: await authApi.me(), status: "ready" });
    } catch (error) {
      // 401 simply means "not signed in"; anything else leaves the visitor anonymous too.
      if (!(error instanceof ApiError && error.isUnauthorized)) console.warn("Could not load the session", error);
      set({ user: null, status: "ready" });
    }
  },

  async login(login, password) {
    const { user, csrfToken } = await authApi.login({ login, password });
    setCsrfToken(csrfToken); // antiforgery tokens are bound to the identity, so the old one is now invalid
    set({ user, status: "ready" });
    return user;
  },

  async register(input) {
    const { user, csrfToken } = await authApi.register(input);
    setCsrfToken(csrfToken);
    set({ user, status: "ready" });
    return user;
  },

  async logout() {
    try {
      const { token } = await authApi.logout();
      setCsrfToken(token);
    } finally {
      set({ user: null });
    }
  },

  setUser: (user) => set({ user }),
  clear: () => set({ user: null }),
}));

// An expired cookie anywhere in the app signs the user out locally, so protected screens redirect to /login.
onUnauthorized(() => {
  if (useSession.getState().user) useSession.getState().clear();
});
