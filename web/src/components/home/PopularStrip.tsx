"use client";

import { GameCard } from "@/components/games/GameCard";
import { useAsync } from "@/hooks/useAsync";
import { gamesApi } from "@/lib/api/endpoints";

/** A taste of the catalog for the landing page. Failing silently is right here: the page works without it. */
export function PopularStrip() {
  const { data } = useAsync(() => gamesApi.search({ sort: "popular", pageSize: 6 }), []);
  if (!data || data.items.length === 0) return null;
  return (
    <section className="space-y-4" aria-label="Popular games">
      <h2 className="text-2xl font-bold">Popular right now</h2>
      <ul className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-6">
        {data.items.map((g) => (
          <li key={g.id}><GameCard game={g} /></li>
        ))}
      </ul>
    </section>
  );
}
