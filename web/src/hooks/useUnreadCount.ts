"use client";

import { useEffect, useState } from "react";
import { notificationsApi } from "@/lib/api/endpoints";
import { useSession } from "@/stores/session";

const POLL_MS = 60_000;

/** Unread notification badge for the signed-in user; refreshes every minute and when the tab regains focus. */
export function useUnreadCount() {
  const userId = useSession((s) => s.user?.id);
  const [count, setCount] = useState(0);

  useEffect(() => {
    if (!userId) {
      setCount(0);
      return;
    }
    let cancelled = false;
    const refresh = () => {
      notificationsApi
        .unreadCount()
        .then((r) => !cancelled && setCount(r.count))
        .catch(() => {}); // a failed poll must never break the page
    };
    refresh();
    const timer = setInterval(refresh, POLL_MS);
    const onFocus = () => document.visibilityState === "visible" && refresh();
    document.addEventListener("visibilitychange", onFocus);
    return () => {
      cancelled = true;
      clearInterval(timer);
      document.removeEventListener("visibilitychange", onFocus);
    };
  }, [userId]);

  return { count, setCount };
}
