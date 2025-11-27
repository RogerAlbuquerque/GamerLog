"use client";

import { useEffect } from "react";
import { Button } from "@/components/ui/Button";

export default function GlobalError({ error, reset }: { error: Error & { digest?: string }; reset: () => void }) {
  useEffect(() => {
    console.error(error);
  }, [error]);
  return (
    <div role="alert" className="mx-auto max-w-lg space-y-4 py-20 text-center">
      <p className="font-display text-5xl font-extrabold text-danger">Game over</p>
      <h1 className="text-2xl font-bold">Something broke on our side</h1>
      <p className="text-muted">Try again — if it keeps happening, come back in a few minutes.</p>
      <Button onClick={reset}>Retry</Button>
    </div>
  );
}
