"use client";

import { useState } from "react";
import { ActivityList } from "@/components/activity/ActivityList";
import { OwnPage } from "@/components/layout/OwnPage";
import { LinkButton } from "@/components/ui/Button";
import { Pagination } from "@/components/ui/Pagination";
import { EmptyState, ErrorState } from "@/components/ui/States";
import { useAsync } from "@/hooks/useAsync";
import { activityApi } from "@/lib/api/endpoints";

function Feed() {
  const [page, setPage] = useState(1);
  const { data, error, loading, reload } = useAsync(() => activityApi.feed({ page, pageSize: 20 }), [page]);
  return (
    <div className="space-y-4">
      {loading && !data && <p role="status" className="text-muted">Loading your feed…</p>}
      {error != null && <ErrorState error={error} onRetry={reload} />}
      {data && data.items.length === 0 && (
        <EmptyState title="Your feed is quiet" description="Follow other players to see what they complete, rate and review." action={<LinkButton href="/discover">Find people and games</LinkButton>} />
      )}
      {data && <ActivityList items={data.items} />}
      {data && <Pagination page={data.page} pageSize={data.pageSize} total={data.total} onPage={setPage} />}
    </div>
  );
}

export default function FeedPage() {
  return <OwnPage title="Activity feed" subtitle="What you and the people you follow have been playing.">{() => <Feed />}</OwnPage>;
}
