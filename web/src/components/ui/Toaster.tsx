"use client";

import { useEffect } from "react";
import { cn } from "@/lib/cn";
import { useToasts, type Toast } from "@/stores/toast";

const tone = {
  success: "border-mint/50 text-mint",
  error: "border-danger/50 text-danger",
  info: "border-accent/50 text-accent-soft",
} as const;

function ToastItem({ toast }: { toast: Toast }) {
  const dismiss = useToasts((s) => s.dismiss);
  useEffect(() => {
    const timer = setTimeout(() => dismiss(toast.id), toast.kind === "error" ? 7000 : 4000);
    return () => clearTimeout(timer);
  }, [toast.id, toast.kind, dismiss]);

  return (
    <div
      role={toast.kind === "error" ? "alert" : "status"}
      className={cn("pointer-events-auto flex animate-fade-up items-start gap-3 rounded-xl border bg-ink-800/95 px-4 py-3 text-sm shadow-xl backdrop-blur", tone[toast.kind])}
    >
      <p className="flex-1 text-fg">{toast.message}</p>
      <button type="button" onClick={() => dismiss(toast.id)} aria-label="Dismiss notification" className="text-muted hover:text-fg">
        ✕
      </button>
    </div>
  );
}

export function Toaster() {
  const toasts = useToasts((s) => s.toasts);
  return (
    <div aria-live="polite" className="pointer-events-none fixed inset-x-0 bottom-4 z-50 mx-auto flex w-[min(94vw,26rem)] flex-col gap-2">
      {toasts.map((t) => (
        <ToastItem key={t.id} toast={t} />
      ))}
    </div>
  );
}
