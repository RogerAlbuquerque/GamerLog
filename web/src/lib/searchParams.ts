import type { GameSearchParams } from "@/lib/api/types";

const SORTS = ["popular", "rating", "release", "title"] as const;
type Sort = (typeof SORTS)[number];

/** URL → typed search params. Anything unexpected is dropped, never passed through to the API. */
export function parseSearchParams(params: URLSearchParams): GameSearchParams & { page: number } {
  const sort = params.get("sort");
  const year = Number(params.get("year"));
  const page = Number(params.get("page"));
  return {
    q: params.get("q")?.slice(0, 100) || undefined,
    genre: params.get("genre") || undefined,
    platform: params.get("platform") || undefined,
    year: Number.isInteger(year) && year >= 1950 && year <= 2100 ? year : undefined,
    sort: (SORTS as readonly string[]).includes(sort ?? "") ? (sort as Sort) : undefined,
    page: Number.isInteger(page) && page >= 1 ? page : 1,
  };
}

/** Typed search params → URL query string (defaults are omitted so URLs stay short and shareable). */
export function toSearchString(p: GameSearchParams): string {
  const out = new URLSearchParams();
  if (p.q) out.set("q", p.q);
  if (p.genre) out.set("genre", p.genre);
  if (p.platform) out.set("platform", p.platform);
  if (p.year) out.set("year", String(p.year));
  if (p.sort && p.sort !== "popular") out.set("sort", p.sort);
  if (p.page && p.page > 1) out.set("page", String(p.page));
  const text = out.toString();
  return text ? `?${text}` : "";
}
