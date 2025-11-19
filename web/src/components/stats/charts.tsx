import { cn } from "@/lib/cn";

/** Big number with a caption, used in the statistics grids. */
export function StatTile({ label, value, hint, className }: { label: string; value: string | number; hint?: string; className?: string }) {
  return (
    <div className={cn("surface p-5", className)}>
      <p className="text-xs font-semibold uppercase tracking-wider text-muted">{label}</p>
      <p className="mt-1 font-display text-3xl font-bold tabular-nums">{value}</p>
      {hint && <p className="mt-1 text-xs text-muted">{hint}</p>}
    </div>
  );
}

export interface BarDatum {
  label: string;
  value: number;
}

/** Horizontal bars with the value printed on the bar. A data table is rendered for screen readers. */
export function BarList({ data, caption }: { data: BarDatum[]; caption: string }) {
  const max = Math.max(1, ...data.map((d) => d.value));
  if (data.length === 0) return <p className="text-sm text-muted">Not enough data yet.</p>;
  return (
    <>
      <ul className="space-y-2.5" aria-hidden>
        {data.map((d) => (
          <li key={d.label} className="grid grid-cols-[7rem_1fr_2rem] items-center gap-3 text-sm sm:grid-cols-[9rem_1fr_2.5rem]">
            <span className="truncate text-muted">{d.label}</span>
            <span className="h-2.5 overflow-hidden rounded-full bg-ink-700">
              <span className="block h-full rounded-full bg-gradient-to-r from-accent to-accent-soft" style={{ width: `${(d.value / max) * 100}%` }} />
            </span>
            <span className="text-right tabular-nums">{d.value}</span>
          </li>
        ))}
      </ul>
      <table className="sr-only">
        <caption>{caption}</caption>
        <tbody>
          {data.map((d) => (
            <tr key={d.label}>
              <th scope="row">{d.label}</th>
              <td>{d.value}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </>
  );
}

const MONTHS = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

/** Twelve columns, one per month. */
export function MonthlyChart({ values, highlight }: { values: number[]; highlight?: number | null }) {
  const max = Math.max(1, ...values);
  return (
    <div>
      <div className="flex h-36 items-end gap-1.5 sm:gap-2" role="img" aria-label={`Completions per month: ${values.map((v, i) => `${MONTHS[i]} ${v}`).join(", ")}`}>
        {values.map((v, i) => (
          <div key={i} className="flex h-full flex-1 flex-col justify-end">
            {v > 0 && <span className="mb-1 text-center text-[10px] tabular-nums text-muted">{v}</span>}
            <div
              className={cn("w-full rounded-t-md", highlight === i + 1 ? "bg-amber" : "bg-accent/80", v === 0 && "opacity-20")}
              style={{ height: `${v === 0 ? 3 : Math.max(8, (v / max) * 100)}%` }}
            />
          </div>
        ))}
      </div>
      <div className="mt-1 flex gap-1.5 text-[10px] uppercase text-muted sm:gap-2" aria-hidden>
        {MONTHS.map((m) => (
          <span key={m} className="flex-1 text-center">{m}</span>
        ))}
      </div>
    </div>
  );
}

export interface Slice {
  label: string;
  value: number;
  color: string;
}

/** Donut with a legend; `total` goes in the middle. */
export function Donut({ slices, total, centerLabel }: { slices: Slice[]; total: number; centerLabel: string }) {
  const radius = 42;
  const circumference = 2 * Math.PI * radius;
  const sum = slices.reduce((s, x) => s + x.value, 0);
  let offset = 0;
  return (
    <div className="flex flex-wrap items-center gap-6">
      <svg viewBox="0 0 100 100" className="size-40 shrink-0 -rotate-90" role="img" aria-label={slices.map((s) => `${s.label}: ${s.value}`).join(", ")}>
        <circle cx="50" cy="50" r={radius} fill="none" stroke="var(--color-ink-700)" strokeWidth="12" />
        {sum > 0 &&
          slices.filter((s) => s.value > 0).map((s) => {
            const length = (s.value / sum) * circumference;
            const el = <circle key={s.label} cx="50" cy="50" r={radius} fill="none" stroke={s.color} strokeWidth="12" strokeDasharray={`${length} ${circumference - length}`} strokeDashoffset={-offset} />;
            offset += length;
            return el;
          })}
        <g className="rotate-90 origin-center" style={{ transformBox: "fill-box" }}>
          <text x="50" y="50" textAnchor="middle" dominantBaseline="central" fill="var(--color-fg)" fontSize="16" fontWeight="700">{total}</text>
        </g>
      </svg>
      <ul className="space-y-1.5 text-sm">
        {slices.map((s) => (
          <li key={s.label} className="flex items-center gap-2">
            <span className="size-3 rounded-sm" style={{ background: s.color }} aria-hidden />
            <span className="text-muted">{s.label}</span>
            <span className="ml-auto pl-4 tabular-nums">{s.value}</span>
          </li>
        ))}
      </ul>
      <span className="sr-only">{centerLabel}</span>
    </div>
  );
}
