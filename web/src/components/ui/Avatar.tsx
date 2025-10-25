import { cn } from "@/lib/cn";
import { initials } from "@/lib/format";

const sizes = { sm: "size-7 text-[10px]", md: "size-10 text-sm", lg: "size-24 text-2xl" } as const;

/** Uploaded avatar, or initials on a colour derived from the name so the same user always looks the same. */
export function Avatar({ name, src, size = "md", className }: { name: string; src?: string | null; size?: keyof typeof sizes; className?: string }) {
  const hue = [...name].reduce((h, c) => (h * 31 + c.charCodeAt(0)) % 360, 7);
  if (src) {
    // eslint-disable-next-line @next/next/no-img-element -- user media is served by our own API; next/image would proxy it twice
    return <img src={src} alt="" className={cn("shrink-0 rounded-full object-cover ring-1 ring-line", sizes[size], className)} />;
  }
  return (
    <span
      aria-hidden
      style={{ backgroundImage: `linear-gradient(135deg, hsl(${hue} 70% 45%), hsl(${(hue + 50) % 360} 70% 35%))` }}
      className={cn("inline-flex shrink-0 items-center justify-center rounded-full font-semibold text-white ring-1 ring-line", sizes[size], className)}
    >
      {initials(name)}
    </span>
  );
}
