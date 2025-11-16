"use client";

import Link from "next/link";
import { Avatar } from "@/components/ui/Avatar";
import { Cover } from "@/components/ui/Cover";
import { describeActivity } from "@/lib/activityText";
import type { Activity } from "@/lib/api/types";
import { timeAgo } from "@/lib/format";

export function ActivityList({ items }: { items: Activity[] }) {
  return (
    <ul className="space-y-3">
      {items.map((a) => {
        const line = describeActivity(a);
        return (
          <li key={a.id} className="surface flex items-center gap-4 p-4">
            <Link href={`/u/${a.actor.username}`} aria-label={`${a.actor.displayName}'s profile`}>
              <Avatar name={a.actor.displayName} src={a.actor.avatarUrl} />
            </Link>
            <div className="min-w-0 flex-1">
              <p className="text-sm">
                <Link href={`/u/${a.actor.username}`} className="font-semibold hover:text-accent-soft">{a.actor.displayName}</Link>{" "}
                <span className="text-muted">{line.verb}</span>{" "}
                {line.target && (line.href ? <Link href={line.href} className="font-semibold hover:text-accent-soft">{line.target}</Link> : <strong>{line.target}</strong>)}
              </p>
              {a.type === "ReviewPublished" && a.reviewTitle && <p className="truncate text-sm text-muted">“{a.reviewTitle}”</p>}
              <p className="text-xs text-muted">{timeAgo(a.createdAt)}</p>
            </div>
            {a.game && (
              <Link href={`/games/${a.game.slug}`} className="w-12 shrink-0" tabIndex={-1} aria-hidden>
                <Cover src={a.game.coverUrl} title={a.game.title} className="rounded-md" />
              </Link>
            )}
          </li>
        );
      })}
    </ul>
  );
}
