"use client";

import { useState } from "react";
import { Button } from "@/components/ui/Button";
import { Modal } from "@/components/ui/Modal";
import { EmptyState, ErrorState, errorMessage } from "@/components/ui/States";
import { useAsync } from "@/hooks/useAsync";
import { collectionsApi } from "@/lib/api/endpoints";
import { useSession } from "@/stores/session";
import { toast } from "@/stores/toast";
import Link from "next/link";

/** Adds the game to one of the user's own collections. */
export function AddToCollection({ gameId }: { gameId: string }) {
  const user = useSession((s) => s.user)!;
  const [open, setOpen] = useState(false);
  const [busy, setBusy] = useState<string | null>(null);
  const mine = useAsync(() => collectionsApi.forUser(user.username, { pageSize: 50 }), [user.username], open);

  const add = async (id: string, name: string) => {
    setBusy(id);
    try {
      await collectionsApi.addGame(id, gameId);
      toast.success(`Added to “${name}”.`);
      setOpen(false);
    } catch (error) {
      toast.error(errorMessage(error));
    } finally {
      setBusy(null);
    }
  };

  return (
    <>
      <Button variant="secondary" onClick={() => setOpen(true)}>
        + Add to a collection
      </Button>
      <Modal open={open} onClose={() => setOpen(false)} title="Add to collection">
        {mine.error != null && <ErrorState error={mine.error} onRetry={mine.reload} />}
        {mine.data?.items.length === 0 && (
          <EmptyState title="No collections yet" action={<Link href="/collections" className="font-semibold text-accent-soft hover:underline">Create your first collection</Link>} />
        )}
        <ul className="space-y-2">
          {mine.data?.items.map((c) => (
            <li key={c.id} className="flex items-center justify-between gap-3 rounded-xl bg-ink-900/60 px-4 py-3">
              <span className="min-w-0">
                <span className="block truncate font-medium">{c.name}</span>
                <span className="text-xs text-muted">{c.gameCount} games · {c.visibility}</span>
              </span>
              <Button size="sm" loading={busy === c.id} onClick={() => add(c.id, c.name)}>Add</Button>
            </li>
          ))}
        </ul>
      </Modal>
    </>
  );
}
