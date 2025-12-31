"use client";

import Link from "next/link";
import { useSession } from "@/stores/session";
import { Logo } from "./Logo";

export function Footer() {
  const user = useSession((s) => s.user);
  return (
    <footer className="mt-20 border-t border-line/70 bg-ink-950/60">
      <div className="mx-auto flex max-w-7xl flex-col gap-6 px-4 py-10 sm:px-6 md:flex-row md:items-center md:justify-between">
        <div className="space-y-2">
          <Logo />
          <p className="max-w-sm text-sm text-muted">A social journal for the games you play, finish and love. Game data courtesy of IGDB.</p>
        </div>
        <nav aria-label="Footer" className="flex flex-wrap gap-x-6 gap-y-2 text-sm text-muted">
          <Link href="/discover" className="hover:text-fg">Discover</Link>
          <Link href="/search" className="hover:text-fg">Games</Link>
          <Link href="/collections" className="hover:text-fg">Collections</Link>
          {!user && <Link href="/register" className="hover:text-fg">Create an account</Link>}
        </nav>
      </div>
    </footer>
  );
}
