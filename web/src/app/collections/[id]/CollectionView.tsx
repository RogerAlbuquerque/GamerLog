"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useRef, useState } from "react";
import { CollectionForm } from "@/components/collections/CollectionForm";
import { Badge } from "@/components/ui/Badge";
import { Button, LinkButton } from "@/components/ui/Button";
import { Cover } from "@/components/ui/Cover";
import { EmptyState, ErrorState, errorMessage } from "@/components/ui/States";
import { Skeleton } from "@/components/ui/Skeleton";
import { useAsync } from "@/hooks/useAsync";
import { collectionsApi } from "@/lib/api/endpoints";
import { pluralize } from "@/lib/format";
import { moveItem } from "@/lib/reorder";
import { useSession } from "@/stores/session";
import { toast } from "@/stores/toast";

export function CollectionView({ id }: { id: string }) {
  const router = useRouter();
  const user = useSession((s) => s.user);
  const { data, error, loading, reload, setData } = useAsync(() => collectionsApi.get(id), [id, user?.id]);
  const [editing, setEditing] = useState(false);
  const [busy, setBusy] = useState(false);
  const fileRef = useRef<HTMLInputElement>(null);

  if (loading && !data) return <Skeleton className="h-96" />;
  if (error != null || !data) {
    return (error as { status?: number } | undefined)?.status === 404 ? (
      <EmptyState title="Collection not found" description="It may be private or deleted." action={<LinkButton href="/collections">All collections</LinkButton>} />
    ) : (
      <ErrorState error={error} onRetry={reload} />
    );
  }

  const { collection, games } = data;
  const isOwner = user?.id === collection.owner.id;

  const run = async (action: () => Promise<unknown>, success?: string) => {
    setBusy(true);
    try {
      await action();
      if (success) toast.success(success);
    } catch (e) {
      toast.error(errorMessage(e));
      reload();
    } finally {
      setBusy(false);
    }
  };

  const move = (index: number, to: number) => {
    const reordered = moveItem(games, index, to);
    setData({ collection, games: reordered.map((g, i) => ({ ...g, position: i })) }); // optimistic
    void run(async () => setData(await collectionsApi.reorder(id, reordered.map((g) => g.gameId))));
  };

  const remove = (gameId: string, title: string) =>
    run(async () => setData(await collectionsApi.removeGame(id, gameId)), `Removed “${title}”.`);

  const deleteCollection = async () => {
    if (!window.confirm(`Delete “${collection.name}”? This cannot be undone.`)) return;
    await run(async () => {
      await collectionsApi.remove(id);
      router.replace(`/u/${collection.owner.username}/collections`);
    });
  };

  const uploadCover = (file: File | undefined) => {
    if (!file) return;
    const form = new FormData();
    form.append("file", file);
    void run(async () => {
      await collectionsApi.setCover(id, form);
      reload();
    }, "Cover updated.");
  };

  return (
    <div className="space-y-8">
      <header className="surface flex flex-col gap-6 p-6 sm:flex-row sm:items-start">
        {collection.coverUrl && (
          // eslint-disable-next-line @next/next/no-img-element
          <img src={collection.coverUrl} alt="" className="h-40 w-full rounded-xl object-cover sm:w-64" />
        )}
        <div className="min-w-0 flex-1 space-y-2">
          <div className="flex flex-wrap items-center gap-3">
            <h1 className="text-3xl font-bold">{collection.name}</h1>
            {collection.visibility === "Private" && <Badge tone="amber">Private</Badge>}
          </div>
          <p className="text-sm text-muted">
            {pluralize(collection.gameCount, "game")} · by <Link href={`/u/${collection.owner.username}`} className="font-medium text-fg hover:text-accent-soft">{collection.owner.displayName}</Link>
          </p>
          {collection.description && <p className="max-w-2xl whitespace-pre-line break-words text-sm">{collection.description}</p>}
          {isOwner && (
            <div className="flex flex-wrap gap-2 pt-2">
              <Button variant="secondary" size="sm" onClick={() => setEditing(true)}>Edit</Button>
              <Button variant="secondary" size="sm" onClick={() => fileRef.current?.click()} disabled={busy}>Upload cover</Button>
              {collection.coverUrl && <Button variant="ghost" size="sm" disabled={busy} onClick={() => run(async () => { await collectionsApi.removeCover(id); reload(); }, "Cover removed.")}>Remove cover</Button>}
              <Button variant="danger" size="sm" onClick={deleteCollection} disabled={busy}>Delete</Button>
              <input ref={fileRef} type="file" accept="image/png,image/jpeg,image/webp" className="sr-only" aria-label="Collection cover image" onChange={(e) => { uploadCover(e.target.files?.[0]); e.target.value = ""; }} />
            </div>
          )}
        </div>
      </header>

      {games.length === 0 ? (
        <EmptyState title="This collection is empty" description={isOwner ? "Open any game and use “Add to a collection”." : undefined} action={isOwner ? <LinkButton href="/search">Find games</LinkButton> : undefined} />
      ) : (
        <ol className="space-y-3">
          {games.map((g, i) => (
            <li key={g.gameId} className="surface flex items-center gap-4 p-3">
              <span className="w-8 text-center font-display text-lg font-bold text-muted">{i + 1}</span>
              <Link href={`/games/${g.slug}`} className="w-14 shrink-0"><Cover src={g.coverUrl} title={g.title} className="rounded-md" /></Link>
              <div className="min-w-0 flex-1">
                <Link href={`/games/${g.slug}`} className="block truncate font-semibold hover:text-accent-soft">{g.title}</Link>
                {g.releaseYear && <p className="text-xs text-muted">{g.releaseYear}</p>}
              </div>
              {isOwner && (
                <div className="flex items-center gap-1">
                  <Button variant="ghost" size="sm" disabled={busy || i === 0} onClick={() => move(i, i - 1)} aria-label={`Move ${g.title} up`}>↑</Button>
                  <Button variant="ghost" size="sm" disabled={busy || i === games.length - 1} onClick={() => move(i, i + 1)} aria-label={`Move ${g.title} down`}>↓</Button>
                  <Button variant="ghost" size="sm" disabled={busy} onClick={() => remove(g.gameId, g.title)} aria-label={`Remove ${g.title}`}>✕</Button>
                </div>
              )}
            </li>
          ))}
        </ol>
      )}

      {isOwner && <CollectionForm open={editing} existing={collection} onClose={() => setEditing(false)} onSaved={(c) => setData(c)} />}
    </div>
  );
}
