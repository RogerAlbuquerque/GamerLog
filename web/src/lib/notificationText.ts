import type { AppNotification } from "@/lib/api/types";

export interface NotificationLine {
  text: string;
  href: string;
}

/** What a notification says and where it leads. The actor's name is rendered separately (as a link). */
export function describeNotification(n: AppNotification): NotificationLine {
  const game = n.gameTitle ? ` “${n.gameTitle}”` : "";
  switch (n.type) {
    case "NewFollower":
      return { text: "started following you", href: `/u/${n.actor.username}` };
    case "ReviewLiked":
      return { text: `liked your review${n.gameTitle ? ` of “${n.gameTitle}”` : ""}`, href: n.reviewId ? `/reviews/${n.reviewId}` : "/feed" };
    case "ReviewComment":
      return { text: `commented on your review${n.gameTitle ? ` of “${n.gameTitle}”` : ""}`, href: n.reviewId ? `/reviews/${n.reviewId}` : "/feed" };
    case "CommentLiked":
      return { text: "liked your comment", href: n.reviewId ? `/reviews/${n.reviewId}` : n.gameSlug ? `/games/${n.gameSlug}` : "/feed" };
    case "CommentReply":
      return { text: `replied to your comment${game ? ` on${game}` : ""}`, href: n.reviewId ? `/reviews/${n.reviewId}` : n.gameSlug ? `/games/${n.gameSlug}` : "/feed" };
  }
}
