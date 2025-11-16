"use client";

import { use, useState } from "react";
import { ActivityList } from "@/components/activity/ActivityList";
import { Pagination } from "@/components/ui/Pagination";
import { EmptyState, ErrorState } from "@/components/ui/States";
import { useAsync } from "@/hooks/useAsync";
import { activityApi } from "@/lib/api/endpoints";

export default function ProfileActivityPage({ params }: { params: Promise<{ username: string }> }) {
  const { username } = use(params);
  const [page, setPage] = useState(1);
  const { data, error, reload } = useAsync(() => activityApi.forUser(username, { page, pageSize: 15 }), [username, page]);
  return (
    <div className="space-y-4">
      {error != null && <ErrorState error={error} onRetry={reload} />}
      {data && data.items.length === 0 && <EmptyState title="Nothing here yet" description="Completed games, reviews and collections will show up here." />}
      {data && <ActivityList items={data.items} />}
      {data && <Pagination page={data.page} pageSize={data.pageSize} total={data.total} onPage={setPage} />}
    </div>
  );
}
