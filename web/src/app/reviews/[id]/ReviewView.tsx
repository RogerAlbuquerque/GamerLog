"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { CommentThread } from "@/components/comments/CommentThread";
import { ReviewCard } from "@/components/reviews/ReviewCard";
import { Button, LinkButton } from "@/components/ui/Button";
import { Cover } from "@/components/ui/Cover";
import { EmptyState, ErrorState, errorMessage } from "@/components/ui/States";
import { Skeleton } from "@/components/ui/Skeleton";
import { useAsync } from "@/hooks/useAsync";
import { reviewsApi } from "@/lib/api/endpoints";
import { useSession } from "@/stores/session";
import { toast } from "@/stores/toast";

export function ReviewView({ id }: { id: string }) {
  const router = useRouter();
  const user = useSession((s) => s.user);
  const { data: review, error, loading, reload } = useAsync(() => reviewsApi.get(id), [id, user?.id]);

  if (loading) return <Skeleton className="h-96" />;
  if (error != null || !review) {
    return (error as { status?: number } | undefined)?.status === 404 ? (
      <EmptyState title="Review not found" description="It may have been deleted by its author." action={<LinkButton href="/discover">Back to discover</LinkButton>} />
    ) : (
      <ErrorState error={error} onRetry={reload} />
    );
  }

  const canDelete = user && (user.id === review.author.id || user.role === "Admin");
  const remove = async () => {
    if (!window.confirm("Delete this review? This cannot be undone.")) return;
    try {
      await reviewsApi.remove(review.id);
      toast.success("Review deleted.");
      router.replace(`/games/${review.gameSlug}`);
    } catch (e) {
      toast.error(errorMessage(e));
    }
  };

  return (
    <div className="mx-auto grid max-w-5xl gap-8 lg:grid-cols-[14rem_1fr]">
      <aside className="mx-auto w-40 space-y-3 lg:w-full">
        <Link href={`/games/${review.gameSlug}`} className="block"><Cover src={review.coverUrl} title={review.gameTitle} /></Link>
        <Link href={`/games/${review.gameSlug}`} className="block text-center font-semibold hover:text-accent-soft lg:text-left">{review.gameTitle}</Link>
      </aside>
      <div className="space-y-8">
        <ReviewCard review={review} full />
        {canDelete && (
          <div>
            <Button variant="danger" size="sm" onClick={remove}>Delete review</Button>
          </div>
        )}
        <CommentThread target={{ kind: "review", id: review.id }} />
      </div>
    </div>
  );
}
