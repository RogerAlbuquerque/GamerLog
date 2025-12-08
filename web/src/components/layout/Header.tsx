"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useState } from "react";
import type { FormEvent } from "react";
import { Avatar } from "@/components/ui/Avatar";
import { buttonClass } from "@/components/ui/Button";
import { cn } from "@/lib/cn";
import { useUnreadCount } from "@/hooks/useUnreadCount";
import { useSession } from "@/stores/session";
import { toast } from "@/stores/toast";
import { Logo } from "./Logo";

const publicLinks = [
  { href: "/discover", label: "Discover" },
  { href: "/search", label: "Games" },
  { href: "/collections", label: "Collections" },
];
const memberLinks = [
  { href: "/feed", label: "Feed" },
  { href: "/diary", label: "My diary" },
];

function SearchBox() {
  const router = useRouter();
  const [q, setQ] = useState("");
  const submit = (e: FormEvent) => {
    e.preventDefault();
    const term = q.trim();
    router.push(term ? `/search?q=${encodeURIComponent(term)}` : "/search");
  };
  return (
    <form onSubmit={submit} role="search" className="relative w-full max-w-xs">
      <label htmlFor="global-search" className="sr-only">
        Search games
      </label>
      <input
        id="global-search"
        value={q}
        onChange={(e) => setQ(e.target.value)}
        placeholder="Search games…"
        maxLength={100}
        className="w-full rounded-xl border border-line bg-ink-900/80 py-2 pl-9 pr-3 text-sm placeholder:text-muted/70 focus:border-accent focus:outline-none focus:ring-2 focus:ring-accent/30"
      />
      <svg viewBox="0 0 24 24" aria-hidden className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted" fill="none" stroke="currentColor" strokeWidth="2">
        <circle cx="11" cy="11" r="7" />
        <path d="m20 20-3.5-3.5" strokeLinecap="round" />
      </svg>
    </form>
  );
}

function NavLink({ href, label, onClick }: { href: string; label: string; onClick?: () => void }) {
  const pathname = usePathname();
  const active = pathname === href || pathname.startsWith(`${href}/`);
  return (
    <Link
      href={href}
      onClick={onClick}
      aria-current={active ? "page" : undefined}
      className={cn("rounded-lg px-3 py-2 text-sm font-medium", active ? "bg-ink-700 text-fg" : "text-muted hover:text-fg")}
    >
      {label}
    </Link>
  );
}

export function Header() {
  const { user, status, logout } = useSession();
  const router = useRouter();
  const { count } = useUnreadCount();
  const [open, setOpen] = useState(false);
  const close = () => setOpen(false);

  const signOut = async () => {
    close();
    try {
      await logout();
    } catch {
      /* the local session is cleared either way */
    }
    toast.info("You have been signed out.");
    router.push("/");
  };

  const links = user ? [...publicLinks.slice(0, 1), ...memberLinks, ...publicLinks.slice(1)] : publicLinks;

  return (
    <header className="sticky top-0 z-40 border-b border-line/70 bg-ink-900/85 backdrop-blur-md">
      <div className="mx-auto flex h-16 max-w-7xl items-center gap-3 px-4 sm:px-6">
        <Logo />
        <nav aria-label="Main" className="ml-4 hidden items-center gap-1 lg:flex">
          {links.map((l) => (
            <NavLink key={l.href} {...l} />
          ))}
        </nav>
        <div className="ml-auto hidden flex-1 justify-end md:flex">
          <SearchBox />
        </div>

        <div className="ml-auto flex items-center gap-2 md:ml-0">
          {status === "ready" && user ? (
            <>
              <Link href="/notifications" className="relative rounded-xl p-2 text-muted hover:bg-ink-700 hover:text-fg" aria-label={count > 0 ? `Notifications, ${count} unread` : "Notifications"}>
                <svg viewBox="0 0 24 24" className="size-5" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden>
                  <path d="M6 9a6 6 0 1 1 12 0c0 5 2 6 2 6H4s2-1 2-6Z" strokeLinejoin="round" />
                  <path d="M10 19a2 2 0 0 0 4 0" strokeLinecap="round" />
                </svg>
                {count > 0 && (
                  <span className="absolute right-0.5 top-0.5 grid min-w-4 place-items-center rounded-full bg-accent px-1 text-[10px] font-bold text-ink-950">{count > 99 ? "99+" : count}</span>
                )}
              </Link>
              <details className="group relative hidden lg:block">
                <summary className="flex cursor-pointer list-none items-center gap-2 rounded-xl p-1 pr-2 hover:bg-ink-700" aria-label="Account menu">
                  <Avatar name={user.displayName} src={user.avatarUrl} size="sm" />
                  <span className="text-sm font-medium">{user.displayName}</span>
                </summary>
                <div className="surface absolute right-0 mt-2 w-52 space-y-0.5 p-1.5 shadow-xl">
                  <Link href={`/u/${user.username}`} className="block rounded-lg px-3 py-2 text-sm hover:bg-ink-700">Profile</Link>
                  <Link href="/stats" className="block rounded-lg px-3 py-2 text-sm hover:bg-ink-700">Statistics</Link>
                  <Link href="/retrospective" className="block rounded-lg px-3 py-2 text-sm hover:bg-ink-700">Yearly retrospective</Link>
                  <Link href="/settings" className="block rounded-lg px-3 py-2 text-sm hover:bg-ink-700">Settings</Link>
                  {user.role === "Admin" && <Link href="/admin/import" className="block rounded-lg px-3 py-2 text-sm hover:bg-ink-700">Import games</Link>}
                  <button type="button" onClick={signOut} className="block w-full rounded-lg px-3 py-2 text-left text-sm text-danger hover:bg-ink-700">Sign out</button>
                </div>
              </details>
            </>
          ) : status === "ready" ? (
            <div className="hidden items-center gap-2 lg:flex">
              <Link href="/login" className={buttonClass("ghost", "sm")}>Sign in</Link>
              <Link href="/register" className={buttonClass("primary", "sm")}>Join free</Link>
            </div>
          ) : null}

          <button
            type="button"
            className="rounded-xl p-2 text-muted hover:bg-ink-700 hover:text-fg lg:hidden"
            aria-expanded={open}
            aria-controls="mobile-menu"
            aria-label={open ? "Close menu" : "Open menu"}
            onClick={() => setOpen((v) => !v)}
          >
            <svg viewBox="0 0 24 24" className="size-6" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" aria-hidden>
              {open ? <path d="M6 6l12 12M18 6 6 18" /> : <path d="M4 7h16M4 12h16M4 17h16" />}
            </svg>
          </button>
        </div>
      </div>

      {open && (
        <div id="mobile-menu" className="animate-fade-up border-t border-line/70 bg-ink-900 px-4 pb-4 pt-3 lg:hidden">
          <div className="mb-3 md:hidden">
            <SearchBox />
          </div>
          <nav aria-label="Mobile" className="flex flex-col gap-1">
            {links.map((l) => (
              <NavLink key={l.href} {...l} onClick={close} />
            ))}
            {user ? (
              <>
                <NavLink href={`/u/${user.username}`} label="Profile" onClick={close} />
                <NavLink href="/stats" label="Statistics" onClick={close} />
                <NavLink href="/retrospective" label="Yearly retrospective" onClick={close} />
                <NavLink href="/settings" label="Settings" onClick={close} />
                <button type="button" onClick={signOut} className="rounded-lg px-3 py-2 text-left text-sm font-medium text-danger">Sign out</button>
              </>
            ) : (
              <div className="mt-2 flex gap-2">
                <Link href="/login" onClick={close} className={buttonClass("secondary", "md", "flex-1")}>Sign in</Link>
                <Link href="/register" onClick={close} className={buttonClass("primary", "md", "flex-1")}>Join free</Link>
              </div>
            )}
          </nav>
        </div>
      )}
    </header>
  );
}
