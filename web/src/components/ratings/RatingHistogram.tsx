import { StarDisplay } from "@/components/ui/StarRating";
import { formatStars, pluralize } from "@/lib/format";
import type { RatingSummary } from "@/lib/api/types";

/** Community rating: the average plus how the ratings are spread over the ten half-star steps. */
export function RatingHistogram({ summary }: { summary: RatingSummary }) {
  const max = Math.max(1, ...summary.distribution);
  return (
    <section aria-label="Community rating" className="space-y-3">
      <div className="flex items-end gap-4">
        <p className="font-display text-5xl font-bold tabular-nums leading-none">{summary.average == null ? "—" : formatStars(summary.average)}</p>
        <div className="space-y-1 pb-1">
          <StarDisplay value={summary.average} size={18} />
          <p className="text-xs text-muted">{summary.count === 0 ? "No ratings yet" : pluralize(summary.count, "rating")}</p>
        </div>
      </div>
      <div className="flex h-16 items-end gap-1" role="img" aria-label={`Rating distribution: ${summary.distribution.map((n, i) => `${n} at ${(i + 1) / 2} stars`).join(", ")}`}>
        {summary.distribution.map((n, i) => (
          <div key={i} className="group relative flex-1">
            <div
              className="w-full rounded-t bg-accent/70 transition-all group-hover:bg-accent"
              style={{ height: `${Math.max(n === 0 ? 4 : 10, (n / max) * 100)}%`, opacity: n === 0 ? 0.25 : 1 }}
              title={`${n} × ${(i + 1) / 2}★`}
            />
          </div>
        ))}
      </div>
      <div className="flex justify-between text-[10px] uppercase tracking-wider text-muted" aria-hidden>
        <span>½★</span>
        <span>5★</span>
      </div>
    </section>
  );
}
