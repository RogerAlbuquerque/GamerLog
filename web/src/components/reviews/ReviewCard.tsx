"use client";

import Link from "next/link";
import { Avatar } from "@/components/ui/Avatar";
import { Badge } from "@/components/ui/Badge";
import { LikeButton } from "@/components/ui/LikeButton";
import { Prose } from "@/components/ui/Prose";
import { Spoiler } from "@/components/ui/Spoiler";
import { StarDisplay } from "@/components/ui/StarRating";
import { reviewsApi } from "@/lib/api/endpoints";
import type { Review } from "@/lib/api/types";
import { formatDate, formatPlaytime, pluralize } from "@/lib/format";
import { useSession } from "@/stores/session";

const DIFFICULTY: Record<string, string> = { VeryEasy: "Very easy", Easy: "Easy", Medium: "Medium", Hard: "Hard", VeryHard: "Very hard" };

export function ReviewCard({ review, showGame = false, full = false }: { review: Review; showGame?: boolean; full?: boolean }) {
  const reveal = useSession((s) => s.user?.showSpoilersByDefault ?? false);
  const stars = review.ratingHalfStars == null ? null : review.ratingHalfStars / 2;
  const extras = [
    review.experience && ["The experience", review.experience],
    review.pros && ["Highlights", review.pros],
    review.cons && ["Letdowns", review.cons],
    review.favoriteCharacter && ["Favourite character", review.favoriteCharacter],
    review.favoriteMoment && ["Favourite moment", review.favoriteMoment],
  ].filter(Boolean) as [string, string][];

  return (
    <article className="surface space-y-3 p-5">
      <header className="flex items-start gap-3">
        <Link href={`/u/${review.author.username}`} aria-label={`${review.author.displayName}'s profile`}>
          <Avatar name={review.author.displayName} src={review.author.avatarUrl} />
        </Link>
        <div className="min-w-0 flex-1">
          <p className="text-sm">
            <Link href={`/u/${review.author.username}`} className="font-semibold hover:text-accent-soft">{review.author.displayName}</Link>
            {showGame && (
              <>
                <span className="text-muted"> reviewed </span>
                <Link href={`/games/${review.gameSlug}`} className="font-semibold hover:text-accent-soft">{review.gameTitle}</Link>
              </>
            )}
          </p>
          <p className="text-xs text-muted">
            {formatDate(review.createdAt)}
            {review.platform ? ` · ${review.platform}` : ""}
            {review.playtimeMinutes ? ` · ${formatPlaytime(review.playtimeMinutes)} played` : ""}
          </p>
        </div>
        {stars != null && <StarDisplay value={stars} size={16} />}
      </header>

      <h3 className="text-lg font-semibold">
        {full ? review.title : <Link href={`/reviews/${review.id}`} className="hover:text-accent-soft">{review.title}</Link>}
      </h3>

      <div className="flex flex-wrap gap-2">
        {review.containsSpoilers && <Badge tone="amber">Spoilers</Badge>}
        {review.difficulty && <Badge>Difficulty: {DIFFICULTY[review.difficulty]}</Badge>}
        {review.completedOn && <Badge tone="mint">Completed {formatDate(review.completedOn)}</Badge>}
      </div>

      <Spoiler isSpoiler={review.containsSpoilers} revealByDefault={reveal} label="This review contains spoilers.">
        <Prose text={full ? review.body : review.body.length > 400 ? `${review.body.slice(0, 400).trimEnd()}…` : review.body} className="text-sm text-fg/90" />
        {full && extras.length > 0 && (
          <dl className="mt-4 grid gap-3 sm:grid-cols-2">
            {extras.map(([term, text]) => (
              <div key={term} className="rounded-xl bg-ink-900/60 p-3">
                <dt className="text-xs font-semibold uppercase tracking-wider text-muted">{term}</dt>
                <dd className="mt-1 whitespace-pre-line break-words text-sm">{text}</dd>
              </div>
            ))}
          </dl>
        )}
      </Spoiler>

      <footer className="flex items-center gap-3 pt-1">
        <LikeButton liked={review.likedByMe} count={review.likeCount} label="this review" onToggle={(next) => (next ? reviewsApi.like(review.id) : reviewsApi.unlike(review.id))} />
        <Link href={`/reviews/${review.id}`} className="text-sm text-muted hover:text-fg">
          {pluralize(review.commentCount, "comment")}
        </Link>
        {!full && review.body.length > 400 && (
          <Link href={`/reviews/${review.id}`} className="ml-auto text-sm font-medium text-accent-soft hover:underline">
            Read full review
          </Link>
        )}
      </footer>
    </article>
  );
}
