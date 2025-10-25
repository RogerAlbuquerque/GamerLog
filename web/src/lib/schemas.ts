import { z } from "zod";

// These mirror the server rules (UserRules, ReviewRules, CollectionRules). The server stays the authority;
// the client copy only gives instant feedback.

const username = z
  .string()
  .trim()
  .regex(/^[A-Za-z0-9_]{3,24}$/, "3-24 characters: letters, digits or underscore.");

const password = z.string().min(10, "At least 10 characters.").max(128, "At most 128 characters.");

export const loginSchema = z.object({
  login: z.string().trim().min(1, "Enter your username or email."),
  password: z.string().min(1, "Enter your password."),
});
export type LoginForm = z.infer<typeof loginSchema>;

export const registerSchema = z
  .object({
    username,
    email: z.string().trim().email("Enter a valid email address.").max(254),
    displayName: z.string().trim().max(50, "At most 50 characters.").optional().or(z.literal("")),
    password,
    confirmPassword: z.string(),
  })
  .refine((v) => v.password === v.confirmPassword, { message: "Passwords do not match.", path: ["confirmPassword"] })
  .refine((v) => !v.password.toLowerCase().includes(v.username.toLowerCase()) || v.username.length === 0, {
    message: "Password must not contain the username.",
    path: ["password"],
  });
export type RegisterForm = z.infer<typeof registerSchema>;

export const changePasswordSchema = z
  .object({ currentPassword: z.string().min(1, "Enter your current password."), newPassword: password, confirmPassword: z.string() })
  .refine((v) => v.newPassword === v.confirmPassword, { message: "Passwords do not match.", path: ["confirmPassword"] });
export type ChangePasswordForm = z.infer<typeof changePasswordSchema>;

export const profileSchema = z.object({
  displayName: z.string().trim().min(1, "Display name is required.").max(50, "At most 50 characters."),
  bio: z.string().trim().max(500, "At most 500 characters."),
  showSpoilersByDefault: z.boolean(),
});
export type ProfileForm = z.infer<typeof profileSchema>;

const optionalText = (max: number) => z.string().trim().max(max, `At most ${max} characters.`);

export const reviewSchema = z.object({
  title: z.string().trim().min(1, "Give your review a title.").max(120, "At most 120 characters."),
  body: z.string().trim().min(10, "Write at least 10 characters.").max(10000, "At most 10,000 characters."),
  containsSpoilers: z.boolean(),
  halfStars: z.number().int().min(1).max(10).nullable(),
  experience: optionalText(3000),
  pros: optionalText(1000),
  cons: optionalText(1000),
  favoriteCharacter: optionalText(120),
  favoriteMoment: optionalText(500),
  difficulty: z.enum(["VeryEasy", "Easy", "Medium", "Hard", "VeryHard"]).nullable(),
  platformId: z.number().int().nullable(),
});
export type ReviewForm = z.infer<typeof reviewSchema>;

export const collectionSchema = z.object({
  name: z.string().trim().min(1, "Name your collection.").max(100, "At most 100 characters."),
  description: z.string().trim().max(1000, "At most 1,000 characters."),
  visibility: z.enum(["Public", "Private"]),
});
export type CollectionForm = z.infer<typeof collectionSchema>;

export const commentSchema = z.object({
  body: z.string().trim().min(1, "Write something first.").max(2000, "At most 2,000 characters."),
  isSpoiler: z.boolean(),
});
export type CommentForm = z.infer<typeof commentSchema>;

/** Empty form fields arrive as "" — the API wants null for "not set". */
export const blankToNull = (value: string | undefined | null): string | null => {
  const v = value?.trim();
  return v ? v : null;
};

/** Diary form: dates are optional, completion cannot precede the start, playtime is entered as hours + minutes. */
export const diarySchema = z
  .object({
    status: z.enum(["WantToPlay", "Playing", "Completed", "Abandoned", "OnHold"]),
    platformId: z.number().int().nullable(),
    startedOn: z.string().regex(/^(\d{4}-\d{2}-\d{2})?$/, "Use a valid date."),
    completedOn: z.string().regex(/^(\d{4}-\d{2}-\d{2})?$/, "Use a valid date."),
    hours: z.number().int().min(0, "Cannot be negative.").max(99999),
    minutes: z.number().int().min(0).max(59, "0-59."),
    recommended: z.boolean().nullable(),
    notes: z.string().trim().max(2000, "At most 2,000 characters."),
  })
  .refine((v) => !v.startedOn || !v.completedOn || v.completedOn >= v.startedOn, {
    message: "Completion cannot be before the start date.",
    path: ["completedOn"],
  });
export type DiaryForm = z.infer<typeof diarySchema>;
