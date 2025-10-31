import { describe, expect, it, vi } from "vitest";
import { ApiError } from "@/lib/api/client";
import { applyServerError } from "./forms";

describe("applyServerError", () => {
  it("assigns field errors and returns no general message", () => {
    const setError = vi.fn();
    const error = new ApiError(400, "Invalid", null, { email: "Taken", unknown: "x" });
    expect(applyServerError(error, setError, ["email", "password"])).toBeNull();
    expect(setError).toHaveBeenCalledTimes(1);
    expect(setError).toHaveBeenCalledWith("email", { type: "server", message: "Taken" });
  });

  it("returns the message for errors that are not about a field", () => {
    expect(applyServerError(new ApiError(401, "Invalid username or password."), vi.fn(), ["login"])).toBe("Invalid username or password.");
  });

  it("explains network failures", () => {
    expect(applyServerError(new TypeError("fetch failed"), vi.fn(), [])).toMatch(/reach the server/);
  });
});
