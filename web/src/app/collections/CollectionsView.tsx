"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { CollectionCard } from "@/components/collections/CollectionCard";
import { CollectionForm } from "@/components/collections/CollectionForm";
import { Button } from "@/components/ui/Button";
import { Pagination } from "@/components/ui/Pagination";
import { EmptyState, ErrorState } from "@/components/ui/States";
import { useAsync } from "@/hooks/useAsync";
import { collectionsApi } from "@/lib/api/endpoints";
import { useSession } from "@/stores/session";

export function CollectionsView() {
  const router = useRouter();
  const user = useSession((s) => s.user);
  const [creating, setCreating] = useState(false);
  const [page, setPage] = useState(1);
  const mine = useAsync(() => collectionsApi.forUser(user!.username, { pageSize: 50 }), [user?.id], Boolean(user));
  const pub = useAsync(() => collectionsApi.listPublic({ page, pageSize: 12 }), [page]);

  return (
    <div className="space-y-12">
      <header className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-4xl font-bold">Collections</h1>
          <p className="mt-1 max-w-2xl text-muted">Hand-picked lists of games: your all-time favourites, cozy winter picks, hidden gems.</p>
        </div>
        {user ? <Button onClick={() => setCreating(true)}>+ New collection</Button> : null}
      </header>

      {user && (
        <section className="space-y-4" aria-label="Your collections">
          <h2 className="text-2xl font-bold">Yours</h2>
          {mine.error != null && <ErrorState error={mine.error} onRetry={mine.reload} />}
          {mine.data?.items.length === 0 && <EmptyState title="You have no collections yet" description="Create one and start adding games from any game page." action={<Button onClick={() => setCreating(true)}>Create your first collection</Button>} />}
          <ul className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {mine.data?.items.map((c) => <li key={c.id}><CollectionCard collection={c} /></li>)}
          </ul>
        </section>
      )}

      <section className="space-y-4" aria-label="Community collections">
        <h2 className="text-2xl font-bold">From the community</h2>
        {pub.error != null && <ErrorState error={pub.error} onRetry={pub.reload} />}
        {pub.data?.items.length === 0 && <EmptyState title="No public collections yet" />}
        <ul className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {pub.data?.items.map((c) => <li key={c.id}><CollectionCard collection={c} /></li>)}
        </ul>
        {pub.data && <Pagination page={pub.data.page} pageSize={pub.data.pageSize} total={pub.data.total} onPage={setPage} />}
      </section>

      {user && <CollectionForm open={creating} onClose={() => setCreating(false)} onSaved={(c) => router.push(`/collections/${c.collection.id}`)} />}
    </div>
  );
}
