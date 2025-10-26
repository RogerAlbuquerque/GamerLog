"use client";

import Link from "next/link";
import { cn } from "@/lib/cn";

export interface TabLink {
  href: string;
  label: string;
  active: boolean;
  count?: number;
}

/** Navigation tabs (each tab is a real route, so they work without JS and can be linked to). */
export function TabNav({ tabs, label }: { tabs: TabLink[]; label: string }) {
  return (
    <nav aria-label={label} className="-mx-1 overflow-x-auto">
      <ul className="flex min-w-max gap-1 border-b border-line px-1">
        {tabs.map((tab) => (
          <li key={tab.href}>
            <Link
              href={tab.href}
              aria-current={tab.active ? "page" : undefined}
              className={cn(
                "-mb-px inline-flex items-center gap-2 border-b-2 px-3 py-2.5 text-sm font-medium",
                tab.active ? "border-accent text-fg" : "border-transparent text-muted hover:text-fg",
              )}
            >
              {tab.label}
              {tab.count !== undefined && <span className="rounded-full bg-ink-700 px-1.5 text-xs tabular-nums text-muted">{tab.count}</span>}
            </Link>
          </li>
        ))}
      </ul>
    </nav>
  );
}

/** In-page filter pills (state lives in the parent). */
export function Segmented<T extends string>({ value, options, onChange, label }: { value: T; options: { value: T; label: string }[]; onChange: (v: T) => void; label: string }) {
  return (
    <div role="group" aria-label={label} className="inline-flex flex-wrap gap-1 rounded-xl border border-line bg-ink-900/60 p-1">
      {options.map((o) => (
        <button
          key={o.value}
          type="button"
          aria-pressed={o.value === value}
          onClick={() => onChange(o.value)}
          className={cn("rounded-lg px-3 py-1.5 text-sm font-medium transition", o.value === value ? "bg-accent text-ink-950" : "text-muted hover:text-fg")}
        >
          {o.label}
        </button>
      ))}
    </div>
  );
}
