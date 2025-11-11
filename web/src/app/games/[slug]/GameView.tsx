"use client";

import Link from "next/link";
import { useState } from "react";
import { CommentThread } from "@/components/comments/CommentThread";
import { DiaryPanel } from "@/components/diary/DiaryPanel";
import { GameCard } from "@/components/games/GameCard";
import { RatePanel } from "@/components/ratings/RatePanel";
import { RatingHistogram } from "@/components/ratings/RatingHistogram";
import { ReviewCard } from "@/components/reviews/ReviewCard";
import { ReviewForm } from "@/components/reviews/ReviewForm";
import { Badge } from "@/components/ui/Badge";
import { Button, LinkButton } from "@/components/ui/Button";
import { Cover } from "@/components/ui/Cover";
import { Pagination } from "@/components/ui/Pagination";
import { Segmented } from "@/components/ui/Tabs";
import { EmptyState, ErrorState } from "@/components/ui/States";
import { Skeleton } from "@/components/ui/Skeleton";
import { useAsync } from "@/hooks/useAsync";
import { gamesApi, ratingsApi, reviewsApi } from "@/lib/api/endpoints";
import type { RatingSummary } from "@/lib/api/types";
import { formatDate, pluralize } from "@/lib/format";
import { useSession } from "@/stores/session";
import { AddToCollection } from "./AddToCollection";

const REVIEW_PAGE = 8;

function Reviews({ gameId, gameTitle, platforms }: { gameId: string; gameTitle: string; platforms: { id: number; name: string; slug: string }[] }) {
  const user = useSession((s) => s.user);
  const [sort, setSort] = useState<"newest" | "popular">("popular");
  const [page, setPage] = useState(1);
  const [editing, setEditing] = useState(false);
  const { data, error, reload } = useAsync(() => reviewsApi.forGame(gameId, { sort, page, pageSize: REVIEW_PAGE }), [gameId, sort, page]);

  return (
    <section aria-label="Reviews" className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-2xl font-bold">Reviews{data && <span className="ml-2 text-base font-normal text-muted">{data.total}</span>}</h2>
        <div className="flex items-center gap-3">
          <Segmented label="Sort reviews" value={sort} onChange={(v) => { setSort(v); setPage(1); }} options={[{ value: "popular", label: "Popular" }, { value: "newest", label: "Newest" }]} />
          {user ? <Button onClick={() => setEditing(true)}>Write a review</Button> : <LinkButton href={`/login?next=/games`} variant="secondary">Sign in to review</LinkButton>}
        </div>
      </div>
      {error != null && <ErrorState error={error} onRetry={reload} />}
      {data && data.items.length === 0 && <EmptyState title="No reviews yet" description={`Be the first to share what you thought of ${gameTitle}.`} />}
      <div className="grid gap-4 lg:grid-cols-2">
        {data?.items.map((r) => (
          <ReviewCard key={r.id} review={r} />
        ))}
      </div>
      {data && <Pagination page={data.page} pageSize={data.pageSize} total={data.total} onPage={setPage} />}
      {user && <ReviewForm open={editing} gameId={gameId} gameTitle={gameTitle} platforms={platforms} onClose={() => setEditing(false)} onSaved={() => { setPage(1); reload(); }} />}
    </section>
  );
}

export function GameView({ slug }: { slug: string }) {
  const user = useSession((s) => s.user);
  const { data: game, error, loading, reload } = useAsync(() => gamesApi.get(slug), [slug]);
  const related = useAsync(() => gamesApi.related(slug), [slug]);
  const summary = useAsync(() => ratingsApi.summary(game!.id), [game?.id], Boolean(game));
  const [liveSummary, setLiveSummary] = useState<RatingSummary | null>(null);
  const shownSummary = liveSummary ?? summary.data;

  if (loading) {
    return (
      <div className="grid gap-8 md:grid-cols-[16rem_1fr]" role="status" aria-label="Loading game">
        <Skeleton className="aspect-[3/4]" />
        <div className="space-y-4">
          <Skeleton className="h-10 w-2/3" />
          <Skeleton className="h-5 w-1/3" />
          <Skeleton className="h-32" />
        </div>
      </div>
    );
  }
  if (error != null || !game) {
    const notFound = (error as { status?: number } | undefined)?.status === 404;
    return notFound ? (
      <EmptyState title="Game not found" description="It may have been removed or the link is wrong." action={<LinkButton href="/search">Browse games</LinkButton>} />
    ) : (
      <ErrorState error={error} onRetry={reload} />
    );
  }

  return (
    <div className="space-y-12">
      <section className="relative -mx-4 overflow-hidden rounded-3xl border border-line sm:mx-0">
        {game.backgroundUrl && (
          // eslint-disable-next-line @next/next/no-img-element
          <img src={game.backgroundUrl} alt="" className="absolute inset-0 size-full object-cover opacity-30" />
        )}
        <div className="absolute inset-0 bg-gradient-to-t from-ink-900 via-ink-900/80 to-ink-900/40" aria-hidden />
        <div className="relative grid gap-8 p-6 md:grid-cols-[14rem_1fr] md:p-10">
          <div className="mx-auto w-44 md:w-full">
            <Cover src={game.coverUrl} title={game.title} priority className="shadow-2xl shadow-black/60" />
          </div>
          <div className="space-y-4">
            <div>
              <h1 className="text-4xl font-bold leading-tight md:text-5xl">{game.title}</h1>
              <p className="mt-2 text-muted">
                {game.releaseDate && <span>{formatDate(game.releaseDate)}</span>}
                {game.developer && <span> · {game.developer}</span>}
                {game.publisher && game.publisher !== game.developer && <span> · published by {game.publisher}</span>}
              </p>
            </div>
            <div className="flex flex-wrap gap-2">
              {game.genres.map((g) => (
                <Link key={g.id} href={`/search?genre=${g.slug}`}><Badge tone="accent">{g.name}</Badge></Link>
              ))}
              {game.platforms.map((p) => (
                <Link key={p.id} href={`/search?platform=${p.slug}`}><Badge>{p.name}</Badge></Link>
              ))}
              {game.gameModes.map((m) => (
                <Badge key={m} tone="mint">{m}</Badge>
              ))}
            </div>
            {game.synopsis && <p className="max-w-3xl whitespace-pre-line leading-relaxed text-fg/90">{game.synopsis}</p>}
            <p className="text-sm text-muted">{pluralize(game.completedCount, "player")} completed this game.</p>
            {game.officialUrl && (
              <a href={game.officialUrl} target="_blank" rel="noopener noreferrer nofollow" className="inline-block text-sm font-medium text-accent-soft hover:underline">
                Official website ↗
              </a>
            )}
          </div>
        </div>
      </section>

      <div className="grid gap-6 lg:grid-cols-[1fr_1.4fr]">
        <div className="surface space-y-6 p-6">
          {shownSummary ? <RatingHistogram summary={shownSummary} /> : <Skeleton className="h-40" />}
          {user ? (
            <RatePanel gameId={game.id} onChanged={setLiveSummary} />
          ) : (
            <p className="text-sm text-muted">
              <Link href={`/login?next=/games/${game.slug}`} className="font-semibold text-accent-soft hover:underline">Sign in</Link> to rate this game.
            </p>
          )}
        </div>
        <div className="surface space-y-4 p-6">
          <h2 className="text-xl font-bold">Your diary</h2>
          {user ? (
            <>
              <DiaryPanel gameId={game.id} platforms={game.platforms} />
              <AddToCollection gameId={game.id} />
            </>
          ) : (
            <EmptyState title="Track this game" description="Log your progress, playtime and playthroughs." action={<LinkButton href={`/login?next=/games/${game.slug}`}>Sign in to start</LinkButton>} />
          )}
        </div>
      </div>

      <Reviews gameId={game.id} gameTitle={game.title} platforms={game.platforms} />
      <CommentThread target={{ kind: "game", id: game.id }} heading="Discussion" />

      {related.data && related.data.length > 0 && (
        <section className="space-y-4">
          <h2 className="text-2xl font-bold">You might also like</h2>
          <ul className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-6">
            {related.data.slice(0, 6).map((g) => (
              <li key={g.id}><GameCard game={g} /></li>
            ))}
          </ul>
        </section>
      )}
    </div>
  );
}
