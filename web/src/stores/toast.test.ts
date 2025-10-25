import { beforeEach, describe, expect, it } from "vitest";
import { toast, useToasts } from "./toast";

beforeEach(() => useToasts.setState({ toasts: [] }));

describe("toast store", () => {
  it("adds and dismisses toasts", () => {
    const id = toast.success("Saved");
    expect(useToasts.getState().toasts).toEqual([{ id, kind: "success", message: "Saved" }]);
    useToasts.getState().dismiss(id);
    expect(useToasts.getState().toasts).toHaveLength(0);
  });

  it("keeps only the four most recent toasts", () => {
    for (let i = 0; i < 6; i++) toast.info(`n${i}`);
    expect(useToasts.getState().toasts.map((t) => t.message)).toEqual(["n2", "n3", "n4", "n5"]);
  });

  it("gives every toast a unique id", () => {
    const a = toast.error("a");
    const b = toast.error("b");
    expect(a).not.toBe(b);
  });
});
