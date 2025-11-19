import Link from "next/link";
import { Badge } from "@/components/ui/Badge";
import { Cover } from "@/components/ui/Cover";
import type { CollectionSummary } from "@/lib/api/types";
import { pluralize } from "@/lib/format";

/** A collection as a stack of its first covers, so a list of collections is recognisable at a glance. */
export function CollectionCard({ collection }: { collection: CollectionSummary }) {
  const covers = collection.previewCovers.slice(0, 3);
  return (
    <Link href={`/collections/${collection.id}`} className="surface group block overflow-hidden transition hover:border-accent/50">
      <div className="relative h-36 overflow-hidden bg-ink-900">
        {collection.coverUrl && covers.length === 0 ? (
          // eslint-disable-next-line @next/next/no-img-element
          <img src={collection.coverUrl} alt="" className="size-full object-cover" />
        ) : (
          <div className="flex h-full items-center justify-center gap-[-1rem] px-6">
            {covers.length === 0 ? (
              <Cover src={null} title={collection.name} className="h-28 w-20" />
            ) : (
              covers.map((src, i) => (
                // eslint-disable-next-line @next/next/no-img-element
                <img key={src} src={src} alt="" className="-ml-6 h-28 w-20 rounded-lg object-cover shadow-lg ring-2 ring-ink-800 first:ml-0" style={{ zIndex: 3 - i }} />
              ))
            )}
          </div>
        )}
      </div>
      <div className="space-y-1 p-4">
        <div className="flex items-start justify-between gap-2">
          <h3 className="line-clamp-1 font-semibold group-hover:text-accent-soft">{collection.name}</h3>
          {collection.visibility === "Private" && <Badge tone="amber">Private</Badge>}
        </div>
        <p className="text-xs text-muted">{pluralize(collection.gameCount, "game")} · by {collection.owner.displayName}</p>
        {collection.description && <p className="line-clamp-2 text-sm text-muted">{collection.description}</p>}
      </div>
    </Link>
  );
}
