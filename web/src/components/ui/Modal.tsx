"use client";

import { useEffect, useRef } from "react";
import type { ReactNode } from "react";
import { cn } from "@/lib/cn";

interface ModalProps {
  open: boolean;
  title: string;
  onClose: () => void;
  children: ReactNode;
  className?: string;
}

/** Built on the native <dialog>: focus trapping, Escape-to-close and inert background come from the browser. */
export function Modal({ open, title, onClose, children, className }: ModalProps) {
  const ref = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    const dialog = ref.current;
    if (!dialog) return;
    if (open && !dialog.open) {
      if (typeof dialog.showModal === "function") dialog.showModal();
      else dialog.setAttribute("open", "");
    } else if (!open && dialog.open) {
      dialog.close?.();
    }
  }, [open]);

  return (
    <dialog
      ref={ref}
      aria-label={title}
      onClose={onClose}
      onClick={(e) => {
        if (e.target === ref.current) onClose(); // click on the backdrop
      }}
      className={cn(
        "m-auto w-[min(92vw,38rem)] rounded-2xl border border-line bg-ink-800 p-0 text-fg shadow-2xl backdrop:bg-ink-950/75 backdrop:backdrop-blur-sm",
        className,
      )}
    >
      {open && (
        <div className="max-h-[85dvh] overflow-y-auto p-6">
          <div className="mb-4 flex items-start justify-between gap-4">
            <h2 className="text-xl font-semibold">{title}</h2>
            <button type="button" onClick={onClose} aria-label="Close dialog" className="rounded-lg p-1 text-muted hover:bg-ink-700 hover:text-fg">
              ✕
            </button>
          </div>
          {children}
        </div>
      )}
    </dialog>
  );
}
