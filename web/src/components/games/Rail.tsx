import type { ReactNode } from "react";

/** Titled horizontal shelf of games; scrolls on small screens instead of squeezing the cards. */
export function Rail({ title, subtitle, children, action }: { title: string; subtitle?: string; children: ReactNode; action?: ReactNode }) {
  return (
    <section className="space-y-4">
      <div className="flex items-end justify-between gap-4">
        <div>
          <h2 className="text-2xl font-bold">{title}</h2>
          {subtitle && <p className="text-sm text-muted">{subtitle}</p>}
        </div>
        {action}
      </div>
      <div className="-mx-4 overflow-x-auto px-4 pb-3 sm:-mx-6 sm:px-6">{children}</div>
    </section>
  );
}

export function RailList({ children }: { children: ReactNode }) {
  return <ul className="flex min-w-max snap-x gap-4">{children}</ul>;
}
