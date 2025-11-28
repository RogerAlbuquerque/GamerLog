import { Spinner } from "@/components/ui/Spinner";

export default function Loading() {
  return (
    <div className="grid min-h-[40vh] place-items-center" role="status" aria-live="polite">
      <Spinner />
      <span className="sr-only">Loading…</span>
    </div>
  );
}
