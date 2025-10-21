/** Playtime is stored in minutes; show it the way players talk about it ("42h 30m"). */
export function formatPlaytime(minutes: number | null | undefined): string {
  if (!minutes || minutes <= 0) return "0h";
  const h = Math.floor(minutes / 60);
  const m = minutes % 60;
  if (h === 0) return `${m}m`;
  return m === 0 ? `${h}h` : `${h}h ${m}m`;
}

/** Ratings are stored as half-stars (1-10); the UI shows stars (0.5-5). */
export function halfStarsToStars(halfStars: number | null | undefined): number | null {
  return halfStars == null ? null : halfStars / 2;
}

export function formatStars(stars: number | null | undefined): string {
  if (stars == null) return "—";
  return Number.isInteger(stars) ? stars.toFixed(1) : String(stars);
}

const dateFormat = new Intl.DateTimeFormat("en", { year: "numeric", month: "short", day: "numeric", timeZone: "UTC" });

/** Dates from the API are UTC; format them in UTC so server and client render identically (no hydration mismatch). */
export function formatDate(value: string | Date | null | undefined): string {
  if (!value) return "";
  const date = typeof value === "string" ? new Date(value.length === 10 ? `${value}T00:00:00Z` : value) : value;
  return Number.isNaN(date.getTime()) ? "" : dateFormat.format(date);
}

export function timeAgo(value: string, now: Date = new Date()): string {
  const then = new Date(value.endsWith("Z") || value.includes("+") ? value : `${value}Z`).getTime();
  const seconds = Math.max(0, Math.round((now.getTime() - then) / 1000));
  if (seconds < 60) return "just now";
  const units: [number, string][] = [
    [60 * 60 * 24 * 365, "y"],
    [60 * 60 * 24 * 30, "mo"],
    [60 * 60 * 24 * 7, "w"],
    [60 * 60 * 24, "d"],
    [60 * 60, "h"],
    [60, "m"],
  ];
  for (const [size, label] of units) {
    if (seconds >= size) return `${Math.floor(seconds / size)}${label} ago`;
  }
  return "just now";
}

export function pluralize(count: number, singular: string, plural = `${singular}s`): string {
  return `${count} ${count === 1 ? singular : plural}`;
}

export function initials(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) return "?";
  return (parts[0][0] + (parts.length > 1 ? parts[parts.length - 1][0] : "")).toUpperCase();
}
