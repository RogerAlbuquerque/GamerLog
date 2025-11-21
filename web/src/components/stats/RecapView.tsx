"use client";

import Link from "next/link";
import { useState } from "react";
import { Cover } from "@/components/ui/Cover";
import { Select } from "@/components/ui/Field";
import { StarDisplay } from "@/components/ui/StarRating";
import { EmptyState, ErrorState } from "@/components/ui/States";
import { Skeleton } from "@/components/ui/Skeleton";
import { useAsync } from "@/hooks/useAsync";
import { statsApi } from "@/lib/api/endpoints";
import type { RecapGame } from "@/lib/api/types";
import { formatDate, formatPlaytime, formatStars, pluralize } from "@/lib/format";
import { BarList, MonthlyChart, StatTile } from "./charts";
import { ShareCard } from "./ShareCard";

const MONTHS = ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];

function Highlight({ title, game }: { title: string; game: RecapGame | null }) {
  if (!game) return null;
  return (
    <div className="surface flex items-center gap-4 p-4">
      <Cover src={game.coverUrl} title={game.title} className="w-14 shrink-0 rounded-md" />
      <div className="min-w-0">
        <p className="text-xs font-semibold uppercase tracking-wider text-muted">{title}</p>
        <Link href={`/games/${game.slug}`} className="block truncate font-semibold hover:text-accent-soft">{game.title}</Link>
        <p className="text-xs text-muted">{formatPlaytime(game.playtimeMinutes)}{game.completedOn ? ` · ${formatDate(game.completedOn)}` : ""}</p>
      </div>
    </div>
  );
}

export function RecapView({ username, displayName }: { username: string; displayName: string }) {
  const [year, setYear] = useState<number | undefined>(undefined);
  const { data, error, loading, reload } = useAsync(() => statsApi.recap(username, year), [username, year]);

  const years = data ? Array.from(new Set([data.year, ...data.availableYears])).sort((a, b) => b - a) : [];

  return (
    <div className="space-y-8">
      {years.length > 1 && (
        <div className="max-w-40">
          <Select label="Year" value={year ?? data?.year ?? ""} onChange={(e) => setYear(Number(e.target.value))}>
            {years.map((y) => <option key={y} value={y}>{y}</option>)}
          </Select>
        </div>
      )}
      {loading && !data && <Skeleton className="h-96" />}
      {error != null && <ErrorState error={error} onRetry={reload} />}
      {data && data.gamesCompleted === 0 && (
        <EmptyState title={`No completed games in ${data.year}`} description="Mark a game as completed with a completion date and it will appear in your retrospective." />
      )}
      {data && data.gamesCompleted > 0 && (
        <div className="grid gap-8 xl:grid-cols-[1fr_24rem]">
          <div className="space-y-8">
            <div className="rounded-3xl border border-accent/30 bg-gradient-to-br from-accent/20 via-ink-800 to-ink-900 p-8">
              <p className="text-sm font-semibold uppercase tracking-widest text-accent-soft">{displayName}&apos;s year in games</p>
              <h2 className="font-display text-6xl font-extrabold sm:text-7xl">{data.year}</h2>
              <p className="mt-3 max-w-xl text-lg text-fg/90">
                You finished <strong>{pluralize(data.gamesCompleted, "game")}</strong> and spent <strong>{formatPlaytime(data.totalPlaytimeMinutes)}</strong> playing
                {data.busiestMonth ? <>, with <strong>{MONTHS[data.busiestMonth - 1]}</strong> your busiest month</> : null}.
              </p>
            </div>

            <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
              <StatTile label="Completed" value={data.gamesCompleted} hint={data.completions > data.gamesCompleted ? `${data.completions} runs` : undefined} />
              <StatTile label="Time played" value={formatPlaytime(data.totalPlaytimeMinutes)} />
              <StatTile label="Avg rating" value={data.averageRating == null ? "—" : `${formatStars(data.averageRating)}★`} />
              <StatTile label="Reviews" value={data.reviewsWritten} hint={`${data.ratingsGiven} ratings`} />
            </div>

            <section className="surface space-y-4 p-6" aria-label="Completions per month">
              <h3 className="text-lg font-bold">Month by month</h3>
              <MonthlyChart values={data.completedByMonth} highlight={data.busiestMonth} />
            </section>

            <div className="grid gap-6 md:grid-cols-2">
              <section className="surface space-y-4 p-6" aria-label="Top genres"><h3 className="text-lg font-bold">Genres</h3><BarList caption="Completed games per genre" data={data.topGenres.map((g) => ({ label: g.name, value: g.count }))} /></section>
              <section className="surface space-y-4 p-6" aria-label="Top platforms"><h3 className="text-lg font-bold">Platforms</h3><BarList caption="Completed games per platform" data={data.topPlatforms.map((g) => ({ label: g.name, value: g.count }))} /></section>
            </div>

            <div className="grid gap-4 sm:grid-cols-2">
              <Highlight title="Longest game" game={data.longestGame} />
              <Highlight title="Shortest game" game={data.shortestGame} />
              <Highlight title="First finished" game={data.firstCompleted} />
              <Highlight title="Latest finished" game={data.lastCompleted} />
            </div>

            {data.highestRated.length > 0 && (
              <section className="space-y-3" aria-label="Top rated of the year">
                <h3 className="text-lg font-bold">Your top rated</h3>
                <ol className="grid gap-4 sm:grid-cols-3">
                  {data.highestRated.map((g, i) => (
                    <li key={g.gameId} className="surface space-y-2 p-4">
                      <span className="font-display text-2xl font-bold text-accent-soft">#{i + 1}</span>
                      <Cover src={g.coverUrl} title={g.title} />
                      <Link href={`/games/${g.slug}`} className="block truncate font-semibold hover:text-accent-soft">{g.title}</Link>
                      <StarDisplay value={g.halfStars == null ? null : g.halfStars / 2} size={14} />
                    </li>
                  ))}
                </ol>
              </section>
            )}
          </div>
          <aside aria-label="Shareable card" className="xl:sticky xl:top-24 xl:self-start">
            <h3 className="mb-3 text-lg font-bold">Share your year</h3>
            <ShareCard recap={data} displayName={displayName} />
          </aside>
        </div>
      )}
    </div>
  );
}
