"use client";

import Link from "next/link";
import { useState } from "react";
import { OwnPage } from "@/components/layout/OwnPage";
import { Avatar } from "@/components/ui/Avatar";
import { Button } from "@/components/ui/Button";
import { Pagination } from "@/components/ui/Pagination";
import { Segmented } from "@/components/ui/Tabs";
import { EmptyState, ErrorState, errorMessage } from "@/components/ui/States";
import { useAsync } from "@/hooks/useAsync";
import { notificationsApi } from "@/lib/api/endpoints";
import { timeAgo } from "@/lib/format";
import { describeNotification } from "@/lib/notificationText";
import { cn } from "@/lib/cn";
import { toast } from "@/stores/toast";

function Notifications() {
  const [filter, setFilter] = useState<"all" | "unread">("all");
  const [page, setPage] = useState(1);
  const { data, error, reload } = useAsync(() => notificationsApi.list({ unreadOnly: filter === "unread", page, pageSize: 20 }), [filter, page]);

  const markAll = async () => {
    try {
      await notificationsApi.markAllRead();
      reload();
    } catch (e) {
      toast.error(errorMessage(e));
    }
  };

  const open = (id: string, isRead: boolean) => {
    if (!isRead) void notificationsApi.markRead(id).catch(() => {});
  };

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <Segmented label="Filter notifications" value={filter} onChange={(v) => { setFilter(v); setPage(1); }} options={[{ value: "all", label: "All" }, { value: "unread", label: "Unread" }]} />
        <Button variant="secondary" size="sm" onClick={markAll}>Mark all as read</Button>
      </div>
      {error != null && <ErrorState error={error} onRetry={reload} />}
      {data && data.items.length === 0 && <EmptyState title={filter === "unread" ? "All caught up" : "No notifications yet"} description="You will be told about new followers, likes and replies." />}
      <ul className="space-y-2">
        {data?.items.map((n) => {
          const line = describeNotification(n);
          return (
            <li key={n.id}>
              <Link href={line.href} onClick={() => open(n.id, n.isRead)} className={cn("surface flex items-center gap-4 p-4 transition hover:border-accent/50", !n.isRead && "border-accent/40 bg-accent/5")}>
                <Avatar name={n.actor.displayName} src={n.actor.avatarUrl} />
                <p className="min-w-0 flex-1 text-sm">
                  <strong>{n.actor.displayName}</strong> {line.text}
                  <span className="block text-xs text-muted">{timeAgo(n.createdAt)}</span>
                </p>
                {!n.isRead && <span className="size-2.5 rounded-full bg-accent" role="img" aria-label="Unread" />}
              </Link>
            </li>
          );
        })}
      </ul>
      {data && <Pagination page={data.page} pageSize={data.pageSize} total={data.total} onPage={setPage} />}
    </div>
  );
}

export default function NotificationsPage() {
  return <OwnPage title="Notifications">{() => <Notifications />}</OwnPage>;
}
