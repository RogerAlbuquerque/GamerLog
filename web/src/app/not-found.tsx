import { LinkButton } from "@/components/ui/Button";

export default function NotFound() {
  return (
    <div className="mx-auto max-w-lg space-y-4 py-20 text-center">
      <p className="font-display text-7xl font-extrabold text-accent">404</p>
      <h1 className="text-2xl font-bold">This level doesn&apos;t exist</h1>
      <p className="text-muted">The page you were looking for has moved or never existed.</p>
      <LinkButton href="/">Back to the start screen</LinkButton>
    </div>
  );
}
