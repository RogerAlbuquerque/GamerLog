import Link from "next/link";
import type { AnchorHTMLAttributes, ButtonHTMLAttributes } from "react";
import { cn } from "@/lib/cn";
import { Spinner } from "./Spinner";

type Variant = "primary" | "secondary" | "ghost" | "danger";
type Size = "sm" | "md" | "lg";

const variants: Record<Variant, string> = {
  primary: "bg-accent text-ink-950 hover:bg-accent-soft shadow-[0_0_24px_-6px] shadow-accent/60",
  secondary: "border border-line bg-ink-700 text-fg hover:border-accent/60 hover:bg-ink-600",
  ghost: "text-muted hover:bg-ink-700 hover:text-fg",
  danger: "bg-danger/90 text-ink-950 hover:bg-danger",
};
const sizes: Record<Size, string> = { sm: "px-3 py-1.5 text-xs", md: "px-4 py-2 text-sm", lg: "px-6 py-3 text-base" };

export const buttonClass = (variant: Variant = "primary", size: Size = "md", className?: string) =>
  cn("btn", variants[variant], sizes[size], className);

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant;
  size?: Size;
  loading?: boolean;
}

export function Button({ variant = "primary", size = "md", loading, disabled, className, children, type = "button", ...rest }: ButtonProps) {
  return (
    <button type={type} disabled={disabled || loading} aria-busy={loading || undefined} className={buttonClass(variant, size, className)} {...rest}>
      {loading && <Spinner className="size-4" />}
      {children}
    </button>
  );
}

interface LinkButtonProps extends AnchorHTMLAttributes<HTMLAnchorElement> {
  href: string;
  variant?: Variant;
  size?: Size;
}

export function LinkButton({ variant = "primary", size = "md", className, href, ...rest }: LinkButtonProps) {
  return <Link href={href} className={buttonClass(variant, size, className)} {...rest} />;
}
