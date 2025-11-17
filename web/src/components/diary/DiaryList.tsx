"use client";

import Link from "next/link";
import { useState } from "react";
import { Badge } from "@/components/ui/Badge";
import { Cover } from "@/components/ui/Cover";
import { Pagination } from "@/components/ui/Pagination";
import { StarDisplay } from "@/components/ui/StarRating";
import { Segmented } from "@/components/ui/Tabs";
import { EmptyState, ErrorState } from "@/components/ui/States";
import { GridSkeleton } from "@/components/ui/Skeleton";
import { useAsync } from "@/hooks/useAsync";
import { diaryApi } from "@/lib/api/endpoints";
import type { DiaryStatus } from "@/lib/api/types";
import { STATUS_LABELS } from "@/lib/diaryForm";
import { formatDate, formatPlaytime } from "@/lib/format";

const TONE: Record<DiaryStatus, "accent" | "mint" | "amber" | "danger" | "neutral"> = {
  Playing: "accent", Completed: "mint", OnHold: "amber", Abandoned: "danger", WantToPlay: "neutral",
};

type Filter = "All" | DiaryStatus;

export function DiaryList({ username, emptyAction }: { username: string; emptyAction?: React.ReactNode }) {
  const [status, setStatus] = useState<Filter>("All");
  const [page, setPage] = useState(1);
  const { data, error, loading, reload } = useAsync(
    () => diaryApi.list(username, { status: status === "All" ? undefined : status, page, pageSize: 24 }),
    [username, status, page],
  );

  return (
    <div className="space-y-5">
      <Segmented<Filter>
        label="Filter by status"
        value={status}
        onChange={(v) => { setStatus(v); setPage(1); }}
        options={[{ value: "All", label: "All" }, ...Object.entries(STATUS_LABELS).map(([value, label]) => ({ value: value as DiaryStatus, label }))]}
      />
      {loading && !data && <GridSkeleton count={8} />}
      {error != null && <ErrorState error={error} onRetry={reload} />}
      {data && data.items.length === 0 && <EmptyState title="No games here" description={status === "All" ? "Games added to the diary will appear here." : `No games with status “${STATUS_LABELS[status]}”.`} action={emptyAction} />}
      {data && data.items.length > 0 && (
        <ul className="grid grid-cols-2 gap-x-4 gap-y-6 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-6">
          {data.items.map((e) => (
            <li key={e.id}>
              <Link href={`/games/${e.gameSlug}`} className="group block space-y-2">
                <div className="relative overflow-hidden rounded-xl transition group-hover:-translate-y-1">
                  <Cover src={e.coverUrl} title={e.gameTitle} />
                  <Badge tone={TONE[e.status]} className="absolute left-2 top-2 bg-ink-950/80 backdrop-blur">{STATUS_LABELS[e.status]}</Badge>
                </div>
                <div className="space-y-0.5 px-0.5">
                  <h3 className="line-clamp-2 text-sm font-semibold group-hover:text-accent-soft">{e.gameTitle}</h3>
                  <StarDisplay value={e.ratingHalfStars == null ? null : e.ratingHalfStars / 2} size={12} />
                  <p className="text-xs text-muted">
                    {formatPlaytime(e.totalPlaytimeMinutes)}
                    {e.completedOn ? ` · ${formatDate(e.completedOn)}` : ""}
                  </p>
                </div>
              </Link>
            </li>
          ))}
        </ul>
      )}
      {data && <Pagination page={data.page} pageSize={data.pageSize} total={data.total} onPage={setPage} />}
    </div>
  );
}
