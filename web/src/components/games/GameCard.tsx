import Link from "next/link";
import { Cover } from "@/components/ui/Cover";
import { StarDisplay } from "@/components/ui/StarRating";
import type { GameSummary } from "@/lib/api/types";

export function GameCard({ game, note }: { game: GameSummary; note?: string }) {
  const year = game.releaseDate?.slice(0, 4);
  return (
    <Link href={`/games/${game.slug}`} className="group block space-y-2 rounded-2xl focus-visible:outline-offset-4">
      <div className="relative overflow-hidden rounded-xl transition duration-300 group-hover:-translate-y-1 group-hover:shadow-[0_12px_32px_-12px] group-hover:shadow-accent/50">
        <Cover src={game.coverUrl} title={game.title} />
        {game.averageRating != null && (
          <span className="absolute right-2 top-2 rounded-full bg-ink-950/80 px-2 py-0.5 text-xs font-bold tabular-nums text-amber backdrop-blur">
            ★ {game.averageRating.toFixed(1)}
          </span>
        )}
      </div>
      <div className="px-0.5">
        <h3 className="line-clamp-2 text-sm font-semibold leading-snug group-hover:text-accent-soft">{game.title}</h3>
        <p className="mt-0.5 flex items-center gap-2 text-xs text-muted">
          {year && <span>{year}</span>}
          {game.genres[0] && <span className="truncate">· {game.genres[0]}</span>}
        </p>
        {note && <p className="mt-1 text-xs text-mint">{note}</p>}
        {game.averageRating == null && game.ratingCount === 0 && <StarDisplay value={null} size={12} className="mt-1 opacity-60" />}
      </div>
    </Link>
  );
}

export function GameGrid({ games, notes }: { games: GameSummary[]; notes?: Record<string, string> }) {
  return (
    <ul className="grid grid-cols-2 gap-x-4 gap-y-6 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-6">
      {games.map((g) => (
        <li key={g.id}>
          <GameCard game={g} note={notes?.[g.id]} />
        </li>
      ))}
    </ul>
  );
}
