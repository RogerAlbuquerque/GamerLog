"use client";

import Link from "next/link";
import { useState } from "react";
import { Avatar } from "@/components/ui/Avatar";
import { Button } from "@/components/ui/Button";
import { LikeButton } from "@/components/ui/LikeButton";
import { Pagination } from "@/components/ui/Pagination";
import { Prose } from "@/components/ui/Prose";
import { Segmented } from "@/components/ui/Tabs";
import { Spoiler } from "@/components/ui/Spoiler";
import { EmptyState, ErrorState, errorMessage } from "@/components/ui/States";
import { useAsync } from "@/hooks/useAsync";
import { commentsApi } from "@/lib/api/endpoints";
import type { Comment } from "@/lib/api/types";
import { pluralize, timeAgo } from "@/lib/format";
import { useSession } from "@/stores/session";
import { toast } from "@/stores/toast";
import { CommentForm } from "./CommentForm";

export type CommentTarget = { kind: "game"; id: string } | { kind: "review"; id: string };

const PAGE_SIZE = 15;

function CommentItem({ comment, target, canReply, onChanged }: { comment: Comment; target: CommentTarget; canReply: boolean; onChanged: () => void }) {
  const user = useSession((s) => s.user);
  const reveal = user?.showSpoilersByDefault ?? false;
  const [replying, setReplying] = useState(false);
  const [editing, setEditing] = useState(false);
  const [showReplies, setShowReplies] = useState(false);
  const replies = useAsync(() => commentsApi.replies(comment.id, { pageSize: 50 }), [comment.id, comment.replyCount], showReplies);

  if (comment.isDeleted) {
    return <p className="rounded-xl bg-ink-900/50 px-4 py-3 text-sm italic text-muted">[deleted comment]{comment.replyCount > 0 && ` · ${pluralize(comment.replyCount, "reply", "replies")}`}</p>;
  }

  const author = comment.author!;
  const mine = user?.id === author.id;
  const create = target.kind === "game" ? commentsApi.createOnGame : commentsApi.createOnReview;

  const remove = async () => {
    if (!window.confirm("Delete this comment?")) return;
    try {
      await commentsApi.remove(comment.id);
      toast.success("Comment deleted.");
      onChanged();
    } catch (error) {
      toast.error(errorMessage(error));
    }
  };

  return (
    <li className="space-y-2">
      <div className="flex gap-3">
        <Link href={`/u/${author.username}`} aria-label={`${author.displayName}'s profile`}>
          <Avatar name={author.displayName} src={author.avatarUrl} size="sm" className="mt-1" />
        </Link>
        <div className="min-w-0 flex-1 space-y-1.5">
          <p className="text-sm">
            <Link href={`/u/${author.username}`} className="font-semibold hover:text-accent-soft">{author.displayName}</Link>
            <span className="text-xs text-muted"> · {timeAgo(comment.createdAt)}{comment.editedAt ? " · edited" : ""}</span>
          </p>
          {editing ? (
            <CommentForm
              initial={{ body: comment.body, isSpoiler: comment.isSpoiler }}
              submitLabel="Save"
              autoFocus
              onCancel={() => setEditing(false)}
              onSubmit={async (v) => {
                await commentsApi.edit(comment.id, v);
                setEditing(false);
                onChanged();
              }}
            />
          ) : (
            <Spoiler isSpoiler={comment.isSpoiler} revealByDefault={reveal} label="Spoiler comment.">
              <Prose text={comment.body} className="text-sm" />
            </Spoiler>
          )}
          <div className="flex flex-wrap items-center gap-2 text-xs">
            <LikeButton liked={comment.likedByMe} count={comment.likeCount} label="this comment" onToggle={(next) => (next ? commentsApi.like(comment.id) : commentsApi.unlike(comment.id))} />
            {canReply && (
              <button type="button" className="rounded-full px-2 py-1 text-muted hover:text-fg" onClick={() => setReplying((v) => !v)}>
                Reply
              </button>
            )}
            {mine && !editing && (
              <button type="button" className="rounded-full px-2 py-1 text-muted hover:text-fg" onClick={() => setEditing(true)}>
                Edit
              </button>
            )}
            {(mine || user?.role === "Admin") && (
              <button type="button" className="rounded-full px-2 py-1 text-muted hover:text-danger" onClick={remove}>
                Delete
              </button>
            )}
            {comment.replyCount > 0 && (
              <button type="button" className="rounded-full px-2 py-1 font-medium text-accent-soft hover:underline" aria-expanded={showReplies} onClick={() => setShowReplies((v) => !v)}>
                {showReplies ? "Hide" : "Show"} {pluralize(comment.replyCount, "reply", "replies")}
              </button>
            )}
          </div>
          {replying && (
            <CommentForm
              submitLabel="Reply"
              placeholder={`Reply to ${author.displayName}…`}
              autoFocus
              onCancel={() => setReplying(false)}
              onSubmit={async (v) => {
                await create(target.id, { body: v.body, isSpoiler: v.isSpoiler, parentCommentId: comment.id });
                setReplying(false);
                setShowReplies(true);
                onChanged();
              }}
            />
          )}
        </div>
      </div>
      {showReplies && (
        <div className="ml-11 border-l border-line pl-4">
          {replies.loading && <p className="text-xs text-muted">Loading replies…</p>}
          {replies.error != null && <ErrorState error={replies.error} onRetry={replies.reload} />}
          <ul className="space-y-4">
            {replies.data?.items.map((r) => (
              <CommentItem key={r.id} comment={r} target={target} canReply={false} onChanged={() => { replies.reload(); onChanged(); }} />
            ))}
          </ul>
        </div>
      )}
    </li>
  );
}

/** Threaded discussion for a game or a review: sortable, paginated, one level of replies. */
export function CommentThread({ target, heading = "Comments" }: { target: CommentTarget; heading?: string }) {
  const [sort, setSort] = useState<"newest" | "top">("newest");
  const [page, setPage] = useState(1);
  const load = target.kind === "game" ? commentsApi.forGame : commentsApi.forReview;
  const { data, error, loading, reload } = useAsync(() => load(target.id, { sort, page, pageSize: PAGE_SIZE }), [target.kind, target.id, sort, page]);
  const create = target.kind === "game" ? commentsApi.createOnGame : commentsApi.createOnReview;

  return (
    <section aria-label={heading} className="space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-2xl font-bold">
          {heading}
          {data && <span className="ml-2 text-base font-normal text-muted">{data.total}</span>}
        </h2>
        <Segmented label="Sort comments" value={sort} onChange={(v) => { setSort(v); setPage(1); }} options={[{ value: "newest", label: "Newest" }, { value: "top", label: "Top" }]} />
      </div>

      <CommentForm
        onSubmit={async (v) => {
          await create(target.id, { body: v.body, isSpoiler: v.isSpoiler });
          setPage(1);
          setSort("newest");
          reload();
        }}
      />

      {loading && !data && <p className="text-sm text-muted" role="status">Loading comments…</p>}
      {error != null && <ErrorState error={error} onRetry={reload} />}
      {data && data.items.length === 0 && <EmptyState title="No comments yet" description="Be the first to start the conversation." />}
      {data && data.items.length > 0 && (
        <ul className="space-y-6">
          {data.items.map((c) => (
            <CommentItem key={c.id} comment={c} target={target} canReply onChanged={reload} />
          ))}
        </ul>
      )}
      {data && <Pagination page={data.page} pageSize={data.pageSize} total={data.total} onPage={setPage} />}
      {data && loading && <Button variant="ghost" size="sm" loading disabled>Refreshing</Button>}
    </section>
  );
}
