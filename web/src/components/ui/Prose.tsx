import { cn } from "@/lib/cn";

/**
 * User-written text. It is always rendered as a React text node (escaped), never as HTML, and blank lines
 * become paragraphs, so user content cannot inject markup or scripts.
 */
export function Prose({ text, className }: { text: string; className?: string }) {
  const paragraphs = text.split(/\n{2,}/).map((p) => p.trim()).filter(Boolean);
  return (
    <div className={cn("space-y-3 break-words leading-relaxed", className)}>
      {paragraphs.map((p, i) => (
        <p key={i} className="whitespace-pre-line">
          {p}
        </p>
      ))}
    </div>
  );
}
