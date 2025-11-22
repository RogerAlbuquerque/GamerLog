import { describe, expect, it } from "vitest";
import { moveItem } from "./reorder";

describe("moveItem", () => {
  it("moves an item forward and backward", () => {
    expect(moveItem(["a", "b", "c", "d"], 0, 2)).toEqual(["b", "c", "a", "d"]);
    expect(moveItem(["a", "b", "c", "d"], 3, 1)).toEqual(["a", "d", "b", "c"]);
  });

  it("ignores impossible moves without mutating the input", () => {
    const input = ["a", "b"];
    expect(moveItem(input, 0, 5)).toEqual(["a", "b"]);
    expect(moveItem(input, -1, 0)).toEqual(["a", "b"]);
    expect(moveItem(input, 1, 1)).toEqual(["a", "b"]);
    expect(input).toEqual(["a", "b"]);
  });
});
