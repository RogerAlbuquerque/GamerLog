import { cn } from "@/lib/cn";

/**
 * Game cover with a generated fallback (gradient + title) so a missing image never leaves a hole in a grid.
 * Plain <img> on purpose: covers come from our own /api/v1/media route and are already re-encoded.
 */
export function Cover({ src, title, className, priority }: { src: string | null | undefined; title: string; className?: string; priority?: boolean }) {
  if (src) {
    // eslint-disable-next-line @next/next/no-img-element
    return <img src={src} alt={`${title} cover`} loading={priority ? "eager" : "lazy"} decoding="async" className={cn("aspect-[3/4] w-full rounded-xl bg-ink-700 object-cover", className)} />;
  }
  const hue = [...title].reduce((h, c) => (h * 31 + c.charCodeAt(0)) % 360, 11);
  return (
    <div
      role="img"
      aria-label={`${title} (no cover)`}
      style={{ backgroundImage: `linear-gradient(160deg, hsl(${hue} 55% 28%), hsl(${(hue + 70) % 360} 60% 14%))` }}
      className={cn("flex aspect-[3/4] w-full items-end rounded-xl p-3 ring-1 ring-line", className)}
    >
      <span className="font-display text-sm font-semibold leading-tight text-white/90">{title}</span>
    </div>
  );
}
