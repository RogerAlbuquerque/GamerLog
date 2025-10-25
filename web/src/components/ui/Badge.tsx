import type { HTMLAttributes } from "react";
import { cn } from "@/lib/cn";

const tones = {
  neutral: "border-line bg-ink-700/70 text-muted",
  accent: "border-accent/40 bg-accent/15 text-accent-soft",
  mint: "border-mint/40 bg-mint/10 text-mint",
  amber: "border-amber/40 bg-amber/10 text-amber",
  danger: "border-danger/40 bg-danger/10 text-danger",
} as const;

export function Badge({ tone = "neutral", className, ...rest }: HTMLAttributes<HTMLSpanElement> & { tone?: keyof typeof tones }) {
  return (
    <span
      className={cn("inline-flex items-center gap-1 rounded-full border px-2.5 py-0.5 text-xs font-medium", tones[tone], className)}
      {...rest}
    />
  );
}
