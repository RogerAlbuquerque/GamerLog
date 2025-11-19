"use client";

import { use, useState } from "react";
import { CollectionCard } from "@/components/collections/CollectionCard";
import { Pagination } from "@/components/ui/Pagination";
import { EmptyState, ErrorState } from "@/components/ui/States";
import { useAsync } from "@/hooks/useAsync";
import { collectionsApi } from "@/lib/api/endpoints";
import { useSession } from "@/stores/session";

export default function ProfileCollectionsPage({ params }: { params: Promise<{ username: string }> }) {
  const { username } = use(params);
  const me = useSession((s) => s.user);
  const [page, setPage] = useState(1);
  const { data, error, reload } = useAsync(() => collectionsApi.forUser(username, { page, pageSize: 12 }), [username, page, me?.id]);
  return (
    <div className="space-y-5">
      {error != null && <ErrorState error={error} onRetry={reload} />}
      {data && data.items.length === 0 && <EmptyState title="No collections yet" description="Public collections will show up here." />}
      <ul className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {data?.items.map((c) => <li key={c.id}><CollectionCard collection={c} /></li>)}
      </ul>
      {data && <Pagination page={data.page} pageSize={data.pageSize} total={data.total} onPage={setPage} />}
    </div>
  );
}
