"use client";

import { useState } from "react";
import type { ReactNode } from "react";
import { Button } from "./Button";

/**
 * Hides spoiler content until the reader opts in. The content is not rendered at all while hidden (not merely
 * blurred), so it cannot be read by assistive tech or by selecting text.
 */
export function Spoiler({ isSpoiler, revealByDefault = false, label = "This contains spoilers.", children }: { isSpoiler: boolean; revealByDefault?: boolean; label?: string; children: ReactNode }) {
  const [shown, setShown] = useState(false);
  if (!isSpoiler || revealByDefault || shown) return <>{children}</>;
  return (
    <div className="flex flex-wrap items-center gap-3 rounded-xl border border-amber/30 bg-amber/5 px-4 py-3 text-sm">
      <span aria-hidden>⚠️</span>
      <span className="flex-1 text-amber">{label}</span>
      <Button variant="secondary" size="sm" aria-expanded={false} onClick={() => setShown(true)}>
        Show anyway
      </Button>
    </div>
  );
}
