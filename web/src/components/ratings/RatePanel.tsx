"use client";

import { useEffect, useState } from "react";
import { StarInput } from "@/components/ui/StarRating";
import { ApiError } from "@/lib/api/client";
import { ratingsApi } from "@/lib/api/endpoints";
import type { RatingSummary } from "@/lib/api/types";
import { formatStars } from "@/lib/format";
import { toast } from "@/stores/toast";
import { errorMessage } from "@/components/ui/States";

/** The signed-in user's own rating. Saves immediately; a second tap on the same value removes the rating. */
export function RatePanel({ gameId, onChanged }: { gameId: string; onChanged: (summary: RatingSummary) => void }) {
  const [value, setValue] = useState<number | null>(null);
  const [loaded, setLoaded] = useState(false);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    let cancelled = false;
    ratingsApi
      .mine(gameId)
      .then((r) => !cancelled && setValue(r.halfStars))
      .catch((e) => {
        if (!(e instanceof ApiError && e.isNotFound)) console.warn(e);
      })
      .finally(() => !cancelled && setLoaded(true));
    return () => {
      cancelled = true;
    };
  }, [gameId]);

  const change = async (next: number | null) => {
    const previous = value;
    setValue(next); // optimistic
    setSaving(true);
    try {
      if (next == null) await ratingsApi.remove(gameId);
      else await ratingsApi.set(gameId, next);
      onChanged(await ratingsApi.summary(gameId));
    } catch (error) {
      setValue(previous);
      toast.error(errorMessage(error));
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="space-y-1">
      <p className="text-sm font-medium">Your rating</p>
      <StarInput value={value} onChange={change} disabled={!loaded || saving} />
      <p className="text-xs text-muted" aria-live="polite">
        {value == null ? "Tap a star — either half counts." : `${formatStars(value / 2)} out of 5. Tap the same star again to remove.`}
      </p>
    </div>
  );
}
