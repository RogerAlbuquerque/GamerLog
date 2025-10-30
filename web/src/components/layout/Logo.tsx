import Link from "next/link";

export function Logo({ className }: { className?: string }) {
  return (
    <Link href="/" className={`inline-flex items-center gap-2 font-display text-xl font-bold tracking-tight ${className ?? ""}`} aria-label="GameLog home">
      <svg viewBox="0 0 32 32" className="size-7" aria-hidden>
        <rect x="2" y="7" width="28" height="18" rx="9" fill="var(--color-accent)" />
        <path d="M10 13v6M7 16h6" stroke="var(--color-ink-950)" strokeWidth="2.2" strokeLinecap="round" />
        <circle cx="21" cy="14.5" r="1.7" fill="var(--color-ink-950)" />
        <circle cx="24.5" cy="17.5" r="1.7" fill="var(--color-ink-950)" />
      </svg>
      <span>
        Game<span className="text-accent">Log</span>
      </span>
    </Link>
  );
}
