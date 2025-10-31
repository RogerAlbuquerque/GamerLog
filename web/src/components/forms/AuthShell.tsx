import type { ReactNode } from "react";

export function AuthShell({ title, subtitle, children, footer }: { title: string; subtitle: string; children: ReactNode; footer: ReactNode }) {
  return (
    <div className="mx-auto grid max-w-5xl items-center gap-10 py-6 md:grid-cols-2 md:py-14">
      <div className="hidden space-y-4 md:block">
        <p className="text-sm font-semibold uppercase tracking-widest text-accent-soft">Your gaming journal</p>
        <h2 className="text-4xl font-bold leading-tight lg:text-5xl">Every game you finish deserves a story.</h2>
        <p className="max-w-md text-muted">Log playthroughs, rate and review, build collections and look back on your year in games.</p>
      </div>
      <div className="surface animate-fade-up p-6 sm:p-8">
        <h1 className="text-2xl font-bold">{title}</h1>
        <p className="mb-6 mt-1 text-sm text-muted">{subtitle}</p>
        {children}
        <div className="mt-6 border-t border-line pt-4 text-center text-sm text-muted">{footer}</div>
      </div>
    </div>
  );
}
