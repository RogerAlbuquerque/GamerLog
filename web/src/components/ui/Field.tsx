import { forwardRef, useId } from "react";
import type { InputHTMLAttributes, ReactNode, SelectHTMLAttributes, TextareaHTMLAttributes } from "react";
import { cn } from "@/lib/cn";

const control =
  "w-full rounded-xl border border-line bg-ink-900/80 px-3.5 py-2.5 text-sm text-fg placeholder:text-muted/70 " +
  "transition focus:border-accent focus:outline-none focus:ring-2 focus:ring-accent/30 disabled:opacity-60 " +
  "aria-[invalid=true]:border-danger aria-[invalid=true]:ring-danger/20";

interface FieldShellProps {
  label: string;
  error?: string;
  hint?: string;
  children: (props: { id: string; "aria-invalid": boolean; "aria-describedby": string | undefined }) => ReactNode;
  className?: string;
}

/** Label + control + hint/error wiring, so every form field is announced correctly by screen readers. */
function FieldShell({ label, error, hint, children, className }: FieldShellProps) {
  const id = useId();
  const describedBy = error ? `${id}-error` : hint ? `${id}-hint` : undefined;
  return (
    <div className={cn("space-y-1.5", className)}>
      <label htmlFor={id} className="block text-sm font-medium text-fg">
        {label}
      </label>
      {children({ id, "aria-invalid": Boolean(error), "aria-describedby": describedBy })}
      {error ? (
        <p id={`${id}-error`} role="alert" className="text-xs text-danger">
          {error}
        </p>
      ) : hint ? (
        <p id={`${id}-hint`} className="text-xs text-muted">
          {hint}
        </p>
      ) : null}
    </div>
  );
}

interface BaseProps {
  label: string;
  error?: string;
  hint?: string;
  wrapperClassName?: string;
}

export const Input = forwardRef<HTMLInputElement, BaseProps & InputHTMLAttributes<HTMLInputElement>>(function Input(
  { label, error, hint, wrapperClassName, className, ...rest },
  ref,
) {
  return (
    <FieldShell label={label} error={error} hint={hint} className={wrapperClassName}>
      {(a11y) => <input ref={ref} className={cn(control, className)} {...a11y} {...rest} />}
    </FieldShell>
  );
});

export const Textarea = forwardRef<HTMLTextAreaElement, BaseProps & TextareaHTMLAttributes<HTMLTextAreaElement>>(function Textarea(
  { label, error, hint, wrapperClassName, className, rows = 4, ...rest },
  ref,
) {
  return (
    <FieldShell label={label} error={error} hint={hint} className={wrapperClassName}>
      {(a11y) => <textarea ref={ref} rows={rows} className={cn(control, "resize-y", className)} {...a11y} {...rest} />}
    </FieldShell>
  );
});

export const Select = forwardRef<HTMLSelectElement, BaseProps & SelectHTMLAttributes<HTMLSelectElement>>(function Select(
  { label, error, hint, wrapperClassName, className, children, ...rest },
  ref,
) {
  return (
    <FieldShell label={label} error={error} hint={hint} className={wrapperClassName}>
      {(a11y) => (
        <select ref={ref} className={cn(control, "appearance-none pr-8", className)} {...a11y} {...rest}>
          {children}
        </select>
      )}
    </FieldShell>
  );
});

export const Checkbox = forwardRef<HTMLInputElement, { label: string; hint?: string } & Omit<InputHTMLAttributes<HTMLInputElement>, "type">>(function Checkbox(
  { label, hint, className, ...rest },
  ref,
) {
  const id = useId();
  return (
    <div className={cn("flex items-start gap-3", className)}>
      <input
        ref={ref}
        id={id}
        type="checkbox"
        className="mt-0.5 size-4 shrink-0 rounded border-line bg-ink-900 accent-[var(--color-accent)]"
        aria-describedby={hint ? `${id}-hint` : undefined}
        {...rest}
      />
      <label htmlFor={id} className="text-sm">
        <span className="font-medium">{label}</span>
        {hint && (
          <span id={`${id}-hint`} className="block text-xs text-muted">
            {hint}
          </span>
        )}
      </label>
    </div>
  );
});
