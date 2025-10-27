import { act, renderHook, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { useAsync } from "./useAsync";

const deferred = <T,>() => {
  let resolve!: (v: T) => void;
  let reject!: (e: unknown) => void;
  const promise = new Promise<T>((res, rej) => ((resolve = res), (reject = rej)));
  return { promise, resolve, reject };
};

describe("useAsync", () => {
  it("loads data", async () => {
    const { result } = renderHook(() => useAsync(async () => 42, []));
    expect(result.current.loading).toBe(true);
    await waitFor(() => expect(result.current.data).toBe(42));
    expect(result.current.loading).toBe(false);
  });

  it("exposes errors", async () => {
    const { result } = renderHook(() => useAsync(async () => Promise.reject(new Error("nope")), []));
    await waitFor(() => expect(result.current.error).toBeInstanceOf(Error));
    expect(result.current.loading).toBe(false);
  });

  it("ignores a slow response that was superseded by a newer request", async () => {
    const first = deferred<string>();
    const second = deferred<string>();
    const { result, rerender } = renderHook(({ q }) => useAsync(() => (q === "a" ? first.promise : second.promise), [q]), { initialProps: { q: "a" } });
    rerender({ q: "b" });
    await act(async () => second.resolve("fresh"));
    await act(async () => first.resolve("stale"));
    expect(result.current.data).toBe("fresh");
  });

  it("reloads on demand", async () => {
    const load = vi.fn().mockResolvedValueOnce(1).mockResolvedValueOnce(2);
    const { result } = renderHook(() => useAsync(load, []));
    await waitFor(() => expect(result.current.data).toBe(1));
    act(() => result.current.reload());
    await waitFor(() => expect(result.current.data).toBe(2));
  });

  it("does not load while disabled", async () => {
    const load = vi.fn().mockResolvedValue(1);
    const { result } = renderHook(() => useAsync(load, [], false));
    expect(load).not.toHaveBeenCalled();
    expect(result.current.loading).toBe(false);
  });
});
