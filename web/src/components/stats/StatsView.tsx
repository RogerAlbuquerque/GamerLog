"use client";

import Link from "next/link";
import { Cover } from "@/components/ui/Cover";
import { StarDisplay } from "@/components/ui/StarRating";
import { EmptyState, ErrorState } from "@/components/ui/States";
import { Skeleton } from "@/components/ui/Skeleton";
import { useAsync } from "@/hooks/useAsync";
import { statsApi } from "@/lib/api/endpoints";
import type { DiaryStatus } from "@/lib/api/types";
import { STATUS_LABELS } from "@/lib/diaryForm";
import { formatPlaytime, formatStars } from "@/lib/format";
import { BarList, Donut, StatTile } from "./charts";

const STATUS_COLORS: Record<DiaryStatus, string> = {
  Completed: "var(--color-mint)",
  Playing: "var(--color-accent)",
  WantToPlay: "var(--color-ink-500)",
  OnHold: "var(--color-amber)",
  Abandoned: "var(--color-danger)",
};

export function StatsView({ username }: { username: string }) {
  const { data, error, loading, reload } = useAsync(() => statsApi.stats(username), [username]);
  if (loading) return <Skeleton className="h-96" />;
  if (error != null) return <ErrorState error={error} onRetry={reload} />;
  if (!data) return null;
  if (data.totalGames === 0) return <EmptyState title="No statistics yet" description="Add games to the diary to see playtime, ratings and favourite genres." />;

  const ratingMax = Math.max(1, ...data.ratingDistribution);
  return (
    <div className="space-y-8">
      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        <StatTile label="Games logged" value={data.totalGames} />
        <StatTile label="Completed" value={data.completedGames} hint={`${Math.round((data.completedGames / data.totalGames) * 100)}% of the diary`} />
        <StatTile label="Time played" value={formatPlaytime(data.totalPlaytimeMinutes)} />
        <StatTile label="Average rating" value={data.averageRating == null ? "—" : `${formatStars(data.averageRating)}★`} hint={`${data.ratingCount} rated`} />
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        <section className="surface space-y-4 p-6" aria-label="Diary status">
          <h2 className="text-lg font-bold">Diary status</h2>
          <Donut total={data.totalGames} centerLabel="games in the diary" slices={(Object.keys(STATUS_LABELS) as DiaryStatus[]).map((s) => ({ label: STATUS_LABELS[s], value: data.byStatus[s] ?? 0, color: STATUS_COLORS[s] }))} />
        </section>
        <section className="surface space-y-4 p-6" aria-label="Rating distribution">
          <h2 className="text-lg font-bold">How they rate</h2>
          <div className="flex h-32 items-end gap-1.5" role="img" aria-label={`Ratings: ${data.ratingDistribution.map((n, i) => `${n} at ${(i + 1) / 2} stars`).join(", ")}`}>
            {data.ratingDistribution.map((n, i) => (
              <div key={i} className="flex h-full flex-1 flex-col justify-end">
                <div className="w-full rounded-t bg-amber/80" style={{ height: `${n === 0 ? 3 : Math.max(8, (n / ratingMax) * 100)}%`, opacity: n === 0 ? 0.2 : 1 }} title={`${n} × ${(i + 1) / 2}★`} />
              </div>
            ))}
          </div>
          <div className="flex justify-between text-[10px] uppercase text-muted" aria-hidden><span>½★</span><span>5★</span></div>
        </section>
        <section className="surface space-y-4 p-6" aria-label="Top genres">
          <h2 className="text-lg font-bold">Favourite genres</h2>
          <BarList caption="Games per genre" data={data.topGenres.map((g) => ({ label: g.name, value: g.count }))} />
        </section>
        <section className="surface space-y-4 p-6" aria-label="Top platforms">
          <h2 className="text-lg font-bold">Platforms</h2>
          <BarList caption="Games per platform" data={data.topPlatforms.map((g) => ({ label: g.name, value: g.count }))} />
        </section>
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        {[{ title: "Most played", list: data.mostPlayed, meta: (g: (typeof data.mostPlayed)[number]) => formatPlaytime(g.playtimeMinutes) }, { title: "Highest rated", list: data.highestRated, meta: (g: (typeof data.highestRated)[number]) => <StarDisplay value={g.halfStars == null ? null : g.halfStars / 2} size={12} /> }].map((block) => (
          <section key={block.title} className="surface space-y-3 p-6" aria-label={block.title}>
            <h2 className="text-lg font-bold">{block.title}</h2>
            {block.list.length === 0 ? <p className="text-sm text-muted">Nothing yet.</p> : (
              <ol className="space-y-3">
                {block.list.map((g, i) => (
                  <li key={g.gameId} className="flex items-center gap-3">
                    <span className="w-5 text-center font-display font-bold text-muted">{i + 1}</span>
                    <Cover src={g.coverUrl} title={g.title} className="w-10 rounded-md" />
                    <Link href={`/games/${g.slug}`} className="min-w-0 flex-1 truncate font-medium hover:text-accent-soft">{g.title}</Link>
                    <span className="text-sm text-muted">{block.meta(g as never)}</span>
                  </li>
                ))}
              </ol>
            )}
          </section>
        ))}
      </div>
      <p className="text-sm text-muted">{data.reviewCount} reviews written · {data.collectionCount} public collections</p>
    </div>
  );
}
