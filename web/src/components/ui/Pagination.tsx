import { Button } from "./Button";

/** Previous/next paging with a position read-out. Renders nothing when everything fits on one page. */
export function Pagination({ page, pageSize, total, onPage }: { page: number; pageSize: number; total: number; onPage: (page: number) => void }) {
  const pages = Math.max(1, Math.ceil(total / pageSize));
  if (pages <= 1) return null;
  return (
    <nav aria-label="Pagination" className="flex items-center justify-between gap-3 pt-4">
      <Button variant="secondary" size="sm" disabled={page <= 1} onClick={() => onPage(page - 1)}>
        ← Previous
      </Button>
      <p className="text-sm text-muted" aria-live="polite">
        Page {page} of {pages}
      </p>
      <Button variant="secondary" size="sm" disabled={page >= pages} onClick={() => onPage(page + 1)}>
        Next →
      </Button>
    </nav>
  );
}
