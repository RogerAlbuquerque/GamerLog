import { describe, expect, it } from "vitest";
import { formatDate, formatPlaytime, formatStars, halfStarsToStars, initials, pluralize, timeAgo } from "./format";

describe("formatPlaytime", () => {
  it.each([
    [0, "0h"],
    [null, "0h"],
    [45, "45m"],
    [60, "1h"],
    [90, "1h 30m"],
    [2550, "42h 30m"],
  ])("formats %s minutes as %s", (minutes, expected) => {
    expect(formatPlaytime(minutes)).toBe(expected);
  });
});

describe("ratings", () => {
  it("converts half-stars to stars", () => {
    expect(halfStarsToStars(9)).toBe(4.5);
    expect(halfStarsToStars(null)).toBeNull();
  });

  it("formats stars with one decimal only when whole", () => {
    expect(formatStars(4)).toBe("4.0");
    expect(formatStars(3.5)).toBe("3.5");
    expect(formatStars(null)).toBe("—");
  });
});

describe("formatDate", () => {
  it("formats date-only strings without shifting the day", () => {
    expect(formatDate("2025-03-01")).toBe("Mar 1, 2025");
  });

  it("formats timestamps in UTC", () => {
    expect(formatDate("2025-12-31T23:30:00Z")).toBe("Dec 31, 2025");
  });

  it("returns an empty string for missing or invalid values", () => {
    expect(formatDate(null)).toBe("");
    expect(formatDate("not a date")).toBe("");
  });
});

describe("timeAgo", () => {
  const now = new Date("2025-10-20T12:00:00Z");

  it.each([
    ["2025-10-20T11:59:50Z", "just now"],
    ["2025-10-20T11:30:00Z", "30m ago"],
    ["2025-10-20T07:00:00Z", "5h ago"],
    ["2025-10-18T12:00:00Z", "2d ago"],
    ["2025-10-01T12:00:00Z", "2w ago"],
  ])("renders %s as %s", (value, expected) => {
    expect(timeAgo(value, now)).toBe(expected);
  });

  it("treats timestamps without a zone as UTC", () => {
    expect(timeAgo("2025-10-20T11:00:00", now)).toBe("1h ago");
  });
});

describe("pluralize and initials", () => {
  it("pluralizes", () => {
    expect(pluralize(1, "game")).toBe("1 game");
    expect(pluralize(2, "game")).toBe("2 games");
    expect(pluralize(0, "entry", "entries")).toBe("0 entries");
  });

  it("builds initials", () => {
    expect(initials("Roger Albuquerque")).toBe("RA");
    expect(initials("roger")).toBe("R");
    expect(initials("  ")).toBe("?");
  });
});
