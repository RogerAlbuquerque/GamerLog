import { describe, expect, it } from "vitest";
import { safeNext } from "./useRequireAuth";

describe("safeNext", () => {
  it("allows same-site paths", () => {
    expect(safeNext("/diary")).toBe("/diary");
    expect(safeNext("/games/hades?x=1")).toBe("/games/hades?x=1");
  });

  it.each(["https://evil.example", "//evil.example", "/\\evil.example", "javascript:alert(1)", "evil", ""])("rejects %s", (value) => {
    expect(safeNext(value)).toBe("/");
  });

  it("falls back for missing values", () => {
    expect(safeNext(null, "/feed")).toBe("/feed");
    expect(safeNext(undefined)).toBe("/");
  });
});
