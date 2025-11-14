"use client";

import { usePathname } from "next/navigation";
import { useState } from "react";
import type { ReactNode } from "react";
import { Avatar } from "@/components/ui/Avatar";
import { LinkButton } from "@/components/ui/Button";
import { Skeleton } from "@/components/ui/Skeleton";
import { EmptyState, ErrorState } from "@/components/ui/States";
import { TabNav } from "@/components/ui/Tabs";
import { useAsync } from "@/hooks/useAsync";
import { usersApi } from "@/lib/api/endpoints";
import type { FollowState } from "@/lib/api/types";
import { formatDate } from "@/lib/format";
import { useSession } from "@/stores/session";
import { FollowButton } from "./FollowButton";

export function ProfileShell({ username, children }: { username: string; children: ReactNode }) {
  const pathname = usePathname();
  const me = useSession((s) => s.user);
  const profile = useAsync(() => usersApi.profile(username), [username]);
  const follow = useAsync(() => usersApi.followState(username), [username, me?.id]);
  const [override, setOverride] = useState<FollowState | null>(null);
  const state = override ?? follow.data;

  if (profile.loading) return <Skeleton className="h-48" />;
  if (profile.error != null || !profile.data) {
    return (profile.error as { status?: number } | undefined)?.status === 404 ? (
      <EmptyState title="User not found" description="Check the username and try again." action={<LinkButton href="/discover">Back to discover</LinkButton>} />
    ) : (
      <ErrorState error={profile.error} onRetry={profile.reload} />
    );
  }

  const p = profile.data;
  const base = `/u/${p.username}`;
  const tabs = [
    { href: base, label: "Activity" },
    { href: `${base}/diary`, label: "Diary" },
    { href: `${base}/reviews`, label: "Reviews" },
    { href: `${base}/collections`, label: "Collections" },
    { href: `${base}/stats`, label: "Statistics" },
    { href: `${base}/retrospective`, label: "Retrospective" },
  ].map((t) => ({ ...t, active: t.href === base ? pathname === base : pathname.startsWith(t.href) }));

  return (
    <div className="space-y-8">
      <header className="surface flex flex-col items-center gap-6 p-6 text-center sm:flex-row sm:items-start sm:text-left">
        <Avatar name={p.displayName} src={p.avatarUrl} size="lg" />
        <div className="min-w-0 flex-1 space-y-2">
          <div>
            <h1 className="text-3xl font-bold">{p.displayName}</h1>
            <p className="text-sm text-muted">@{p.username} · joined {formatDate(p.joinedAt)}</p>
          </div>
          {p.bio && <p className="max-w-2xl whitespace-pre-line break-words text-sm text-fg/90">{p.bio}</p>}
          {state && (
            <p className="text-sm text-muted">
              <strong className="text-fg">{state.followers}</strong> {state.followers === 1 ? "follower" : "followers"} ·{" "}
              <strong className="text-fg">{state.followingCount}</strong> following
            </p>
          )}
        </div>
        <div className="flex gap-2">
          {state && <FollowButton username={p.username} state={state} onChange={setOverride} />}
          {me?.username.toLowerCase() === p.username.toLowerCase() && <LinkButton href="/settings" variant="secondary">Edit profile</LinkButton>}
        </div>
      </header>
      <TabNav tabs={tabs} label={`${p.displayName}'s profile`} />
      {children}
    </div>
  );
}

