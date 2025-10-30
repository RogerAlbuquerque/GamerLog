"use client";

import { useEffect } from "react";
import type { ReactNode } from "react";
import { Toaster } from "@/components/ui/Toaster";
import { useSession } from "@/stores/session";

/** Resolves the session once on first paint and mounts the toast host. */
export function Providers({ children }: { children: ReactNode }) {
  const load = useSession((s) => s.load);
  useEffect(() => {
    void load();
  }, [load]);
  return (
    <>
      {children}
      <Toaster />
    </>
  );
}
