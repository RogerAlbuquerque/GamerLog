"use client";

import Link from "next/link";
import { useState } from "react";
import { Badge } from "@/components/ui/Badge";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Field";
import { EmptyState, ErrorState, errorMessage } from "@/components/ui/States";
import { useRequireAuth } from "@/hooks/useRequireAuth";
import { adminApi } from "@/lib/api/endpoints";
import type { ExternalGame, ImportedGame } from "@/lib/api/types";
import { toast } from "@/stores/toast";

/** Administrators search IGDB and copy games into the local catalog. Everyone else sees a plain "not allowed". */
export function ImportView() {
  const { user, ready } = useRequireAuth();
  const [query, setQuery] = useState("");
  const [results, setResults] = useState<ExternalGame[] | null>(null);
  const [searching, setSearching] = useState(false);
  const [error, setError] = useState<unknown>(null);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [imported, setImported] = useState<Record<string, ImportedGame>>({});

  if (!ready || !user) return null;
  if (user.role !== "Admin") {
    return <EmptyState title="Administrators only" description="Importing from IGDB is limited to administrators." />;
  }

  const runSearch = async () => {
    const q = query.trim();
    if (q.length < 2) return;
    setSearching(true);
    setError(null);
    try {
      setResults(await adminApi.searchProvider(q));
    } catch (e) {
      setError(e);
    } finally {
      setSearching(false);
    }
  };

  const search = (event: React.FormEvent) => {
    event.preventDefault();
    void runSearch();
  };

  const importOne = async (game: ExternalGame) => {
    setBusyId(game.externalId);
    try {
      const done = await adminApi.importGame(game.externalId);
      setImported((m) => ({ ...m, [game.externalId]: done }));
      toast.success(done.created ? `Imported ${done.title}.` : `${done.title} was refreshed.`);
    } catch (e) {
      toast.error(errorMessage(e));
    } finally {
      setBusyId(null);
    }
  };

  return (
    <div className="space-y-6">
      <header>
        <h1 className="text-4xl font-bold">Import from IGDB</h1>
        <p className="mt-1 text-muted">Search the external database and copy games into the GameLog catalog. Importing again refreshes the existing entry.</p>
      </header>

      <form onSubmit={search} className="flex max-w-xl items-end gap-3" role="search">
        <Input label="Game title" value={query} onChange={(e) => setQuery(e.target.value)} placeholder="e.g. Hades" wrapperClassName="flex-1" />
        <Button type="submit" loading={searching} disabled={query.trim().length < 2}>Search</Button>
      </form>

      {error != null && <ErrorState error={error} onRetry={runSearch} />}
      {results && results.length === 0 && <EmptyState title="No matches" description="Try a different spelling." />}
      {results && results.length > 0 && (
        <ul className="space-y-3">
          {results.map((g) => {
            const done = imported[g.externalId];
            return (
              <li key={g.externalId} className="surface flex flex-wrap items-center gap-4 p-4">
                <div className="min-w-0 flex-1 space-y-1">
                  <p className="font-semibold">
                    {g.title}
                    {g.releaseDate && <span className="ml-2 text-sm font-normal text-muted">{g.releaseDate.slice(0, 4)}</span>}
                  </p>
                  <p className="text-sm text-muted">{[g.developer, g.platforms.slice(0, 4).join(", ")].filter(Boolean).join(" · ")}</p>
                  <div className="flex flex-wrap gap-1.5">{g.genres.slice(0, 4).map((x) => <Badge key={x}>{x}</Badge>)}</div>
                </div>
                {done ? (
                  <Link href={`/games/${done.slug}`} className="text-sm font-semibold text-accent-soft hover:underline">View in catalog →</Link>
                ) : (
                  <Button variant="secondary" size="sm" loading={busyId === g.externalId} onClick={() => importOne(g)}>Import</Button>
                )}
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}
