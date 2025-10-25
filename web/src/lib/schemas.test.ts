import { describe, expect, it } from "vitest";
import { blankToNull, collectionSchema, commentSchema, diarySchema, registerSchema, reviewSchema } from "./schemas";

const validRegister = { username: "roger_a", email: "roger@example.com", displayName: "", password: "correct-horse-9", confirmPassword: "correct-horse-9" };

const messages = (result: { success: boolean; error?: { issues: { message: string }[] } }) =>
  result.success ? [] : result.error!.issues.map((i) => i.message);

describe("registerSchema", () => {
  it("accepts valid input", () => {
    expect(registerSchema.safeParse(validRegister).success).toBe(true);
  });

  it.each(["ab", "has space", "way_too_long_for_a_username_x", "emoji😀"])("rejects username %s", (username) => {
    expect(registerSchema.safeParse({ ...validRegister, username }).success).toBe(false);
  });

  it("requires a long enough password", () => {
    expect(messages(registerSchema.safeParse({ ...validRegister, password: "short", confirmPassword: "short" }))).toContain("At least 10 characters.");
  });

  it("requires matching passwords", () => {
    expect(messages(registerSchema.safeParse({ ...validRegister, confirmPassword: "different-one-1" }))).toContain("Passwords do not match.");
  });

  it("rejects passwords containing the username", () => {
    const p = "my-roger_a-secret";
    expect(messages(registerSchema.safeParse({ ...validRegister, password: p, confirmPassword: p }))).toContain("Password must not contain the username.");
  });

  it("rejects malformed email", () => {
    expect(registerSchema.safeParse({ ...validRegister, email: "nope" }).success).toBe(false);
  });
});

describe("reviewSchema", () => {
  const base = { title: "Great", body: "Really enjoyed it.", containsSpoilers: false, halfStars: null, experience: "", pros: "", cons: "", favoriteCharacter: "", favoriteMoment: "", difficulty: null, platformId: null };

  it("accepts a minimal review", () => {
    expect(reviewSchema.safeParse(base).success).toBe(true);
  });

  it("rejects a short body and a missing title", () => {
    expect(reviewSchema.safeParse({ ...base, body: "short" }).success).toBe(false);
    expect(reviewSchema.safeParse({ ...base, title: "  " }).success).toBe(false);
  });

  it("bounds the star rating to half-stars 1-10", () => {
    expect(reviewSchema.safeParse({ ...base, halfStars: 0 }).success).toBe(false);
    expect(reviewSchema.safeParse({ ...base, halfStars: 11 }).success).toBe(false);
    expect(reviewSchema.safeParse({ ...base, halfStars: 10 }).success).toBe(true);
  });
});

describe("collection and comment schemas", () => {
  it("requires a collection name", () => {
    expect(collectionSchema.safeParse({ name: " ", description: "", visibility: "Public" }).success).toBe(false);
    expect(collectionSchema.safeParse({ name: "Best of 2025", description: "", visibility: "Private" }).success).toBe(true);
  });

  it("rejects empty and oversized comments", () => {
    expect(commentSchema.safeParse({ body: "  ", isSpoiler: false }).success).toBe(false);
    expect(commentSchema.safeParse({ body: "x".repeat(2001), isSpoiler: false }).success).toBe(false);
    expect(commentSchema.safeParse({ body: "ok", isSpoiler: true }).success).toBe(true);
  });
});

describe("diarySchema", () => {
  const base = { status: "Completed", platformId: null, startedOn: "2025-01-01", completedOn: "2025-02-01", hours: 10, minutes: 30, recommended: null, notes: "" };

  it("accepts consistent dates", () => {
    expect(diarySchema.safeParse(base).success).toBe(true);
  });

  it("rejects completion before the start", () => {
    expect(messages(diarySchema.safeParse({ ...base, completedOn: "2024-12-31" }))).toContain("Completion cannot be before the start date.");
  });

  it("allows empty dates and rejects invalid minutes", () => {
    expect(diarySchema.safeParse({ ...base, startedOn: "", completedOn: "" }).success).toBe(true);
    expect(diarySchema.safeParse({ ...base, minutes: 75 }).success).toBe(false);
  });
});

describe("blankToNull", () => {
  it("normalises blanks", () => {
    expect(blankToNull("  ")).toBeNull();
    expect(blankToNull(undefined)).toBeNull();
    expect(blankToNull(" x ")).toBe("x");
  });
});
