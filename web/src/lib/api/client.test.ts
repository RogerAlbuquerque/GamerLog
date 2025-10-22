import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError, buildQuery, http, onUnauthorized, setCsrfToken, toApiError } from "./client";

const json = (status: number, body: unknown) =>
  new Response(JSON.stringify(body), { status, headers: { "content-type": "application/problem+json" } });

describe("buildQuery", () => {
  it("skips empty values and encodes the rest", () => {
    expect(buildQuery({ q: "dark souls", page: 2, genre: "", year: undefined, flag: false })).toBe("?q=dark+souls&page=2&flag=false");
    expect(buildQuery({})).toBe("");
    expect(buildQuery()).toBe("");
  });
});

describe("toApiError", () => {
  it("uses the problem title and code", () => {
    const e = toApiError(409, { title: "Taken", code: "username_taken" });
    expect(e).toBeInstanceOf(ApiError);
    expect(e.message).toBe("Taken");
    expect(e.code).toBe("username_taken");
  });

  it("flattens validation errors to camelCase field names", () => {
    const e = toApiError(400, { title: "Invalid", errors: { Title: ["Required"], body: ["Too short", "x"] } });
    expect(e.fieldErrors).toEqual({ title: "Required", body: "Too short" });
  });

  it("falls back to friendly messages", () => {
    expect(toApiError(500, null).message).toMatch(/our side/);
    expect(toApiError(429, null).message).toMatch(/slow down/);
  });
});

describe("http", () => {
  const fetchMock = vi.fn();

  beforeEach(() => {
    vi.stubGlobal("fetch", fetchMock);
    setCsrfToken(null);
    onUnauthorized(null);
  });

  afterEach(() => {
    fetchMock.mockReset();
    vi.unstubAllGlobals();
  });

  it("does not send a CSRF header on reads", async () => {
    fetchMock.mockResolvedValueOnce(new Response("{}", { status: 200, headers: { "content-type": "application/json" } }));
    await http.get("/games", { q: "hades" });
    const [url, init] = fetchMock.mock.calls[0];
    expect(url).toBe("/api/v1/games?q=hades");
    expect(init.headers["X-CSRF-TOKEN"]).toBeUndefined();
    expect(init.credentials).toBe("same-origin");
  });

  it("fetches a CSRF token once and sends it on writes", async () => {
    fetchMock
      .mockResolvedValueOnce(new Response(JSON.stringify({ token: "tok-1" }), { status: 200, headers: { "content-type": "application/json" } }))
      .mockResolvedValue(new Response(null, { status: 204 }));
    await http.put("/diary/1", { status: "Playing" });
    await http.delete("/diary/1");
    expect(fetchMock.mock.calls[0][0]).toBe("/api/v1/auth/csrf");
    expect(fetchMock.mock.calls[1][1].headers["X-CSRF-TOKEN"]).toBe("tok-1");
    expect(fetchMock.mock.calls[2][1].headers["X-CSRF-TOKEN"]).toBe("tok-1");
    expect(fetchMock).toHaveBeenCalledTimes(3);
  });

  it("retries once with a fresh token when the antiforgery check fails", async () => {
    setCsrfToken("stale");
    fetchMock
      .mockResolvedValueOnce(new Response(null, { status: 400 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ token: "fresh" }), { status: 200, headers: { "content-type": "application/json" } }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ ok: true }), { status: 200, headers: { "content-type": "application/json" } }));
    await expect(http.post("/x", {})).resolves.toEqual({ ok: true });
    expect(fetchMock.mock.calls[2][1].headers["X-CSRF-TOKEN"]).toBe("fresh");
  });

  it("does not retry real validation errors", async () => {
    setCsrfToken("tok");
    fetchMock.mockResolvedValueOnce(json(400, { title: "Invalid", errors: { name: ["Required"] } }));
    await expect(http.post("/x", {})).rejects.toMatchObject({ status: 400, fieldErrors: { name: "Required" } });
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it("notifies the session when a request is unauthorized", async () => {
    const handler = vi.fn();
    onUnauthorized(handler);
    fetchMock.mockResolvedValueOnce(new Response(null, { status: 401 }));
    await expect(http.get("/auth/me")).rejects.toMatchObject({ status: 401 });
    expect(handler).toHaveBeenCalledOnce();
  });

  it("returns undefined for 204 responses", async () => {
    setCsrfToken("tok");
    fetchMock.mockResolvedValueOnce(new Response(null, { status: 204 }));
    await expect(http.delete("/x")).resolves.toBeUndefined();
  });

  it("sends FormData without a JSON content type", async () => {
    setCsrfToken("tok");
    fetchMock.mockResolvedValueOnce(new Response(JSON.stringify({ avatarUrl: "/a" }), { status: 200, headers: { "content-type": "application/json" } }));
    const form = new FormData();
    form.append("file", new Blob(["x"]), "a.png");
    await http.upload("/users/me/avatar", form);
    const init = fetchMock.mock.calls[0][1];
    expect(init.body).toBe(form);
    expect(init.headers["Content-Type"]).toBeUndefined();
  });
});
