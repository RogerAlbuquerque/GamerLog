"use client";

import { usePathname, useRouter } from "next/navigation";
import { useEffect } from "react";
import { useSession } from "@/stores/session";

/** For pages that need a signed-in user: redirects to /login (remembering where to return) once the session is known. */
export function useRequireAuth() {
  const { user, status } = useSession();
  const router = useRouter();
  const pathname = usePathname();

  useEffect(() => {
    if (status === "ready" && !user) router.replace(`/login?next=${encodeURIComponent(pathname)}`);
  }, [status, user, router, pathname]);

  return { user, ready: status === "ready" && Boolean(user) };
}

/** Only same-site relative paths are honoured, so `?next=` can never become an open redirect. */
export function safeNext(next: string | null | undefined, fallback = "/"): string {
  if (!next || !next.startsWith("/") || next.startsWith("//") || next.includes("\\")) return fallback;
  return next;
}
