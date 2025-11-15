import type { Activity } from "@/lib/api/types";

export interface ActivityLine {
  verb: string;
  href: string | null;
  target: string | null;
}

/** Turns a feed item into "<verb> <linked target>". Kept pure so every type is unit tested. */
export function describeActivity(a: Activity): ActivityLine {
  const gameHref = a.game ? `/games/${a.game.slug}` : null;
  switch (a.type) {
    case "GameCompleted":
      return { verb: "completed", href: gameHref, target: a.game?.title ?? null };
    case "GameAddedToDiary":
      return { verb: "added to their diary", href: gameHref, target: a.game?.title ?? null };
    case "GameRated":
      return { verb: `rated ${a.ratingHalfStars != null ? `${a.ratingHalfStars / 2}★` : ""}`.trim(), href: gameHref, target: a.game?.title ?? null };
    case "ReviewPublished":
      return { verb: "reviewed", href: a.reviewId ? `/reviews/${a.reviewId}` : gameHref, target: a.game?.title ?? a.reviewTitle };
    case "CollectionCreated":
      return { verb: "created the collection", href: a.collectionId ? `/collections/${a.collectionId}` : null, target: a.collectionName };
  }
}
