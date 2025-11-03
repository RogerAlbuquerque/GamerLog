import { describe, expect, it } from "vitest";
import { parseSearchParams, toSearchString } from "./searchParams";

describe("parseSearchParams", () => {
  it("reads valid filters", () => {
    const p = parseSearchParams(new URLSearchParams("q=hades&genre=rpg&platform=pc&year=2020&sort=rating&page=3"));
    expect(p).toEqual({ q: "hades", genre: "rpg", platform: "pc", year: 2020, sort: "rating", page: 3 });
  });

  it("drops invalid values", () => {
    const p = parseSearchParams(new URLSearchParams("year=abc&sort=DROP TABLE&page=-4"));
    expect(p).toMatchObject({ year: undefined, sort: undefined, page: 1 });
  });

  it("caps the query length", () => {
    expect(parseSearchParams(new URLSearchParams(`q=${"a".repeat(500)}`)).q).toHaveLength(100);
  });
});

describe("toSearchString", () => {
  it("omits defaults", () => {
    expect(toSearchString({ sort: "popular", page: 1 })).toBe("");
  });

  it("round-trips", () => {
    const original = { q: "dark souls", genre: "rpg", sort: "release" as const, page: 2 };
    const parsed = parseSearchParams(new URLSearchParams(toSearchString(original).slice(1)));
    expect(parsed).toMatchObject(original);
  });
});
