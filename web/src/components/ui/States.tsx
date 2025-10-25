import type { ReactNode } from "react";
import { ApiError } from "@/lib/api/client";
import { Button } from "./Button";

export function EmptyState({ title, description, action }: { title: string; description?: string; action?: ReactNode }) {
  return (
    <div className="surface flex flex-col items-center gap-2 px-6 py-12 text-center">
      <div aria-hidden className="mb-2 grid size-12 place-items-center rounded-2xl bg-ink-700 text-2xl">🎮</div>
      <h3 className="text-lg font-semibold">{title}</h3>
      {description && <p className="max-w-md text-sm text-muted">{description}</p>}
      {action && <div className="mt-3">{action}</div>}
    </div>
  );
}

export function errorMessage(error: unknown): string {
  if (error instanceof ApiError) return error.message;
  return "Something went wrong. Please try again.";
}

export function ErrorState({ error, onRetry }: { error: unknown; onRetry?: () => void }) {
  return (
    <div role="alert" className="surface flex flex-col items-center gap-3 border-danger/40 px-6 py-10 text-center">
      <p className="font-medium">{errorMessage(error)}</p>
      {onRetry && (
        <Button variant="secondary" size="sm" onClick={onRetry}>
          Try again
        </Button>
      )}
    </div>
  );
}
