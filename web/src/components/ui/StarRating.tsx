"use client";

import { useRef } from "react";
import type { KeyboardEvent, PointerEvent } from "react";
import { cn } from "@/lib/cn";
import { formatStars } from "@/lib/format";

const STAR = "M12 2.6l2.8 5.9 6.4.8-4.7 4.4 1.2 6.4L12 17l-5.7 3.1 1.2-6.4L2.8 9.3l6.4-.8L12 2.6z";

function Star({ fill, size }: { fill: number; size: number }) {
  return (
    <span className="relative inline-block" style={{ width: size, height: size }}>
      <svg viewBox="0 0 24 24" width={size} height={size} className="absolute inset-0 text-ink-600" aria-hidden>
        <path d={STAR} fill="currentColor" />
      </svg>
      <span className="absolute inset-0 overflow-hidden" style={{ width: `${fill * 100}%` }}>
        <svg viewBox="0 0 24 24" width={size} height={size} className="text-amber" aria-hidden>
          <path d={STAR} fill="currentColor" />
        </svg>
      </span>
    </span>
  );
}

interface DisplayProps {
  /** Rating in stars (0-5, halves allowed). */
  value: number | null | undefined;
  size?: number;
  className?: string;
  showValue?: boolean;
}

/** Read-only rating. A missing rating renders as empty stars, never as zero stars. */
export function StarDisplay({ value, size = 16, className, showValue }: DisplayProps) {
  const v = value ?? 0;
  return (
    <span className={cn("inline-flex items-center gap-1.5", className)} role="img" aria-label={value == null ? "Not rated" : `Rated ${formatStars(value)} out of 5`}>
      <span className="inline-flex gap-0.5">
        {[0, 1, 2, 3, 4].map((i) => (
          <Star key={i} size={size} fill={Math.min(1, Math.max(0, v - i))} />
        ))}
      </span>
      {showValue && <span className="text-sm font-semibold tabular-nums text-fg">{formatStars(value)}</span>}
    </span>
  );
}

interface InputProps {
  /** Current rating in half-stars (1-10) or null. */
  value: number | null;
  onChange: (halfStars: number | null) => void;
  size?: number;
  disabled?: boolean;
  label?: string;
}

/** Half-star rating input. Click either half of a star; with the keyboard use arrows, Home/End and Delete to clear. */
export function StarInput({ value, onChange, size = 32, disabled, label = "Your rating" }: InputProps) {
  const ref = useRef<HTMLDivElement>(null);
  const current = value ?? 0;

  const fromPointer = (e: PointerEvent<HTMLDivElement>) => {
    const rect = ref.current!.getBoundingClientRect();
    const ratio = Math.min(1, Math.max(0, (e.clientX - rect.left) / rect.width));
    return Math.max(1, Math.ceil(ratio * 10));
  };

  const onKeyDown = (e: KeyboardEvent<HTMLDivElement>) => {
    if (disabled) return;
    const step: Record<string, number | null | undefined> = {
      ArrowRight: Math.min(10, current + 1),
      ArrowUp: Math.min(10, current + 1),
      ArrowLeft: Math.max(1, current - 1),
      ArrowDown: Math.max(1, current - 1),
      Home: 1,
      End: 10,
      Delete: null,
      Backspace: null,
    };
    if (!(e.key in step)) return;
    e.preventDefault();
    onChange(step[e.key] as number | null);
  };

  return (
    <div
      ref={ref}
      role="slider"
      tabIndex={disabled ? -1 : 0}
      aria-label={label}
      aria-valuemin={0}
      aria-valuemax={10}
      aria-valuenow={current}
      aria-valuetext={value == null ? "Not rated" : `${formatStars(current / 2)} out of 5 stars`}
      aria-disabled={disabled}
      onKeyDown={onKeyDown}
      onPointerUp={(e) => {
        if (disabled) return;
        const next = fromPointer(e);
        onChange(next === value ? null : next); // tapping the current value again clears it
      }}
      className={cn("inline-flex w-fit touch-none gap-1 rounded-lg p-1", disabled ? "opacity-50" : "cursor-pointer")}
    >
      {[0, 1, 2, 3, 4].map((i) => (
        <Star key={i} size={size} fill={Math.min(1, Math.max(0, current / 2 - i))} />
      ))}
    </div>
  );
}
