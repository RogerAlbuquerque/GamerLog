"use client";

import type { ReactNode } from "react";
import { Skeleton } from "@/components/ui/Skeleton";
import { useRequireAuth } from "@/hooks/useRequireAuth";

/** Wraps pages about "me": waits for the session, redirects anonymous visitors, then renders with the username. */
export function OwnPage({ title, subtitle, children }: { title: string; subtitle?: string; children: (username: string) => ReactNode }) {
  const { user, ready } = useRequireAuth();
  return (
    <div className="space-y-6">
      <header>
        <h1 className="text-4xl font-bold">{title}</h1>
        {subtitle && <p className="mt-1 text-muted">{subtitle}</p>}
      </header>
      {ready && user ? children(user.username) : <Skeleton className="h-64" />}
    </div>
  );
}
