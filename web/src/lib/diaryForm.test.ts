import { describe, expect, it } from "vitest";
import type { DiaryEntry } from "@/lib/api/types";
import { DEFAULT_DIARY, fromEntry, toRequest, totalMinutes } from "./diaryForm";

const entry = (over: Partial<DiaryEntry> = {}): DiaryEntry => ({
  id: "1", gameId: "g", gameSlug: "hades", gameTitle: "Hades", coverUrl: null, status: "Completed", platformId: 3, platform: "PC",
  totalPlaytimeMinutes: 150, ratingHalfStars: null, recommended: true, notes: "Great", startedOn: "2025-01-01", completedOn: "2025-02-01",
  createdAt: "2025-01-01T00:00:00Z", updatedAt: "2025-01-01T00:00:00Z",
  playthroughs: [{ id: "p", number: 1, startedOn: "2025-01-01", completedOn: "2025-02-01", playtimeMinutes: 150, platformId: 3, platform: "PC", notes: null }],
  ...over,
});

describe("diary form conversion", () => {
  it("adds hours and minutes", () => {
    expect(totalMinutes(2, 30)).toBe(150);
    expect(totalMinutes(0, 0)).toBe(0);
    expect(totalMinutes(-1, 5)).toBe(5);
  });

  it("fills the form from the latest playthrough", () => {
    expect(fromEntry(entry())).toMatchObject({ status: "Completed", hours: 2, minutes: 30, startedOn: "2025-01-01", platformId: 3, recommended: true });
  });

  it("builds a request with null for blanks", () => {
    expect(toRequest({ ...DEFAULT_DIARY, status: "Playing", hours: 1, minutes: 5 })).toEqual({
      status: "Playing", platformId: null, startedOn: null, completedOn: null, playtimeMinutes: 65, recommended: null, notes: null,
    });
  });

  it("drops the completion date unless the game is completed", () => {
    const playing = toRequest({ ...DEFAULT_DIARY, status: "Playing", completedOn: "2025-02-01" });
    const done = toRequest({ ...DEFAULT_DIARY, status: "Completed", completedOn: "2025-02-01" });
    expect(playing.completedOn).toBeNull();
    expect(done.completedOn).toBe("2025-02-01");
  });
});
