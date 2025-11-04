"use client";

import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { useEffect, useMemo, useState } from "react";
import { GameGrid } from "@/components/games/GameCard";
import { Input, Select } from "@/components/ui/Field";
import { Pagination } from "@/components/ui/Pagination";
import { EmptyState, ErrorState } from "@/components/ui/States";
import { GridSkeleton } from "@/components/ui/Skeleton";
import { useAsync } from "@/hooks/useAsync";
import { useDebounced } from "@/hooks/useDebounced";
import { gamesApi } from "@/lib/api/endpoints";
import { parseSearchParams, toSearchString } from "@/lib/searchParams";
import { pluralize } from "@/lib/format";

const PAGE_SIZE = 24;
const YEARS = Array.from({ length: new Date().getUTCFullYear() + 1 - 1980 + 1 }, (_, i) => new Date().getUTCFullYear() + 1 - i);

export function SearchView() {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const params = useMemo(() => parseSearchParams(new URLSearchParams(searchParams.toString())), [searchParams]);

  const [text, setText] = useState(params.q ?? "");
  const debounced = useDebounced(text, 350);

  const update = (patch: Partial<typeof params>) => router.replace(`${pathname}${toSearchString({ ...params, page: 1, ...patch })}`, { scroll: false });

  // Typing updates the URL after a pause; the URL is the single source of truth for the results.
  useEffect(() => {
    if ((params.q ?? "") !== debounced.trim()) update({ q: debounced.trim() || undefined });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [debounced]);

  // Back/forward navigation changes the URL without touching the input.
  useEffect(() => setText(params.q ?? ""), [params.q]);

  const filters = useAsync(async () => ({ genres: await gamesApi.genres(), platforms: await gamesApi.platforms() }), []);
  const results = useAsync(() => gamesApi.search({ ...params, pageSize: PAGE_SIZE }), [searchParams.toString()]);

  return (
    <div className="space-y-6">
      <header>
        <h1 className="text-4xl font-bold">Browse games</h1>
        <p className="mt-1 text-muted">Search the catalog and narrow it down by genre, platform and release year.</p>
      </header>

      <div className="surface grid gap-4 p-4 sm:grid-cols-2 lg:grid-cols-5">
        <Input label="Search" type="search" value={text} onChange={(e) => setText(e.target.value)} maxLength={100} placeholder="Title or developer" wrapperClassName="lg:col-span-2" />
        <Select label="Genre" value={params.genre ?? ""} onChange={(e) => update({ genre: e.target.value || undefined })}>
          <option value="">All genres</option>
          {filters.data?.genres.map((g) => (
            <option key={g.id} value={g.slug}>{g.name}</option>
          ))}
        </Select>
        <Select label="Platform" value={params.platform ?? ""} onChange={(e) => update({ platform: e.target.value || undefined })}>
          <option value="">All platforms</option>
          {filters.data?.platforms.map((p) => (
            <option key={p.id} value={p.slug}>{p.name}</option>
          ))}
        </Select>
        <div className="grid grid-cols-2 gap-3">
          <Select label="Year" value={params.year ?? ""} onChange={(e) => update({ year: e.target.value ? Number(e.target.value) : undefined })}>
            <option value="">Any</option>
            {YEARS.map((y) => (
              <option key={y} value={y}>{y}</option>
            ))}
          </Select>
          <Select label="Sort" value={params.sort ?? "popular"} onChange={(e) => update({ sort: e.target.value as typeof params.sort })}>
            <option value="popular">Popular</option>
            <option value="rating">Top rated</option>
            <option value="release">Newest</option>
            <option value="title">A–Z</option>
          </Select>
        </div>
      </div>

      {results.loading && <GridSkeleton count={12} />}
      {results.error != null && <ErrorState error={results.error} onRetry={results.reload} />}
      {results.data && !results.loading && (
        <>
          <p className="text-sm text-muted" aria-live="polite">{pluralize(results.data.total, "game")} found</p>
          {results.data.items.length === 0 ? (
            <EmptyState title="No games match" description="Try a different spelling or remove a filter." />
          ) : (
            <GameGrid games={results.data.items} />
          )}
          <Pagination page={results.data.page} pageSize={results.data.pageSize} total={results.data.total} onPage={(page) => update({ page })} />
        </>
      )}
    </div>
  );
}
