"use client";

import { use, useState } from "react";
import { ReviewCard } from "@/components/reviews/ReviewCard";
import { Pagination } from "@/components/ui/Pagination";
import { Segmented } from "@/components/ui/Tabs";
import { EmptyState, ErrorState } from "@/components/ui/States";
import { useAsync } from "@/hooks/useAsync";
import { reviewsApi } from "@/lib/api/endpoints";

export default function ProfileReviewsPage({ params }: { params: Promise<{ username: string }> }) {
  const { username } = use(params);
  const [sort, setSort] = useState<"newest" | "popular">("newest");
  const [page, setPage] = useState(1);
  const { data, error, reload } = useAsync(() => reviewsApi.forUser(username, { sort, page, pageSize: 8 }), [username, sort, page]);
  return (
    <div className="space-y-5">
      <Segmented label="Sort reviews" value={sort} onChange={(v) => { setSort(v); setPage(1); }} options={[{ value: "newest", label: "Newest" }, { value: "popular", label: "Popular" }]} />
      {error != null && <ErrorState error={error} onRetry={reload} />}
      {data && data.items.length === 0 && <EmptyState title="No reviews yet" />}
      <div className="grid gap-4 lg:grid-cols-2">
        {data?.items.map((r) => <ReviewCard key={r.id} review={r} showGame />)}
      </div>
      {data && <Pagination page={data.page} pageSize={data.pageSize} total={data.total} onPage={setPage} />}
    </div>
  );
}
