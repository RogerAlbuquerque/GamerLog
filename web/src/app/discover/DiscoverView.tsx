"use client";

import Link from "next/link";
import { GameCard } from "@/components/games/GameCard";
import { Rail, RailList } from "@/components/games/Rail";
import { ErrorState } from "@/components/ui/States";
import { Skeleton } from "@/components/ui/Skeleton";
import { useAsync } from "@/hooks/useAsync";
import { discoveryApi } from "@/lib/api/endpoints";
import type { GameSummary } from "@/lib/api/types";
import { useSession } from "@/stores/session";

function Shelf({ title, subtitle, games, notes }: { title: string; subtitle?: string; games: GameSummary[]; notes?: Record<string, string> }) {
  if (games.length === 0) return null;
  return (
    <Rail title={title} subtitle={subtitle}>
      <RailList>
        {games.map((g) => (
          <li key={g.id} className="w-36 shrink-0 snap-start sm:w-40">
            <GameCard game={g} note={notes?.[g.id]} />
          </li>
        ))}
      </RailList>
    </Rail>
  );
}

function Recommended() {
  const { user, status } = useSession();
  const { data } = useAsync(() => discoveryApi.recommended(12), [user?.id], status === "ready" && Boolean(user));
  if (!user || !data || data.length === 0) return null;
  const notes = Object.fromEntries(data.map((r) => [r.game.id, r.because.length ? `Because you like ${r.because.join(" & ")}` : "Popular with players"]));
  return <Shelf title="Picked for you" subtitle="Based on what you play, finish and rate highly." games={data.map((r) => r.game)} notes={notes} />;
}

export function DiscoverView() {
  const { data, error, loading, reload } = useAsync(() => discoveryApi.overview(), []);

  return (
    <div className="space-y-12">
      <header className="space-y-2">
        <h1 className="text-4xl font-bold">Discover</h1>
        <p className="max-w-2xl text-muted">What players are finishing, rating and talking about right now.</p>
      </header>

      <Recommended />

      {loading && (
        <div className="space-y-4" role="status" aria-label="Loading">
          <Skeleton className="h-8 w-48" />
          <div className="flex gap-4">
            {Array.from({ length: 6 }, (_, i) => (
              <Skeleton key={i} className="aspect-[3/4] w-40 shrink-0" />
            ))}
          </div>
        </div>
      )}
      {error != null && <ErrorState error={error} onRetry={reload} />}
      {data && (
        <>
          <Shelf title="Trending this fortnight" subtitle="Most added to diaries in the last 14 days." games={data.trending} />
          <Shelf title="Popular" subtitle="Rated and completed by the most players." games={data.popular} />
          <Shelf title="Top rated" subtitle="Highest community ratings, weighted by how many people rated." games={data.topRated} />
          <Shelf title="New releases" subtitle="Out in the last four months." games={data.newReleases} />
          <Shelf title="Most completed" subtitle="The games players actually see through to the end." games={data.mostCompleted} />
          <p className="text-sm text-muted">
            Looking for something specific?{" "}
            <Link href="/search" className="font-semibold text-accent-soft hover:underline">
              Browse the full catalog
            </Link>
            .
          </p>
        </>
      )}
    </div>
  );
}
