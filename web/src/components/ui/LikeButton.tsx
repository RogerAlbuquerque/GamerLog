"use client";

import { usePathname, useRouter } from "next/navigation";
import { useState } from "react";
import { cn } from "@/lib/cn";
import { useSession } from "@/stores/session";
import { toast } from "@/stores/toast";
import { errorMessage } from "./States";

interface Props {
  liked: boolean;
  count: number;
  label: string;
  onToggle: (liked: boolean) => Promise<{ liked: boolean; likeCount: number }>;
}

/** Optimistic like toggle; anonymous visitors are sent to sign in and brought back. */
export function LikeButton({ liked: initialLiked, count: initialCount, label, onToggle }: Props) {
  const user = useSession((s) => s.user);
  const router = useRouter();
  const pathname = usePathname();
  const [state, setState] = useState({ liked: initialLiked, count: initialCount });
  const [busy, setBusy] = useState(false);

  const click = async () => {
    if (!user) {
      router.push(`/login?next=${encodeURIComponent(pathname)}`);
      return;
    }
    if (busy) return;
    const previous = state;
    const next = !state.liked;
    setState({ liked: next, count: Math.max(0, state.count + (next ? 1 : -1)) });
    setBusy(true);
    try {
      const result = await onToggle(next);
      setState({ liked: result.liked, count: result.likeCount });
    } catch (error) {
      setState(previous);
      toast.error(errorMessage(error));
    } finally {
      setBusy(false);
    }
  };

  return (
    <button
      type="button"
      onClick={click}
      aria-pressed={state.liked}
      aria-label={`${state.liked ? "Unlike" : "Like"} ${label}`}
      className={cn("inline-flex items-center gap-1.5 rounded-full border px-3 py-1 text-sm transition", state.liked ? "border-accent/60 bg-accent/15 text-accent-soft" : "border-line text-muted hover:border-accent/40 hover:text-fg")}
    >
      <span aria-hidden>{state.liked ? "♥" : "♡"}</span>
      <span className="tabular-nums">{state.count}</span>
    </button>
  );
}
