"use client";

import { usePathname, useRouter } from "next/navigation";
import { useState } from "react";
import { Button } from "@/components/ui/Button";
import { errorMessage } from "@/components/ui/States";
import { usersApi } from "@/lib/api/endpoints";
import type { FollowState } from "@/lib/api/types";
import { useSession } from "@/stores/session";
import { toast } from "@/stores/toast";

export function FollowButton({ username, state, onChange }: { username: string; state: FollowState; onChange: (s: FollowState) => void }) {
  const me = useSession((s) => s.user);
  const router = useRouter();
  const pathname = usePathname();
  const [busy, setBusy] = useState(false);

  if (me?.username.toLowerCase() === username.toLowerCase()) return null;

  const toggle = async () => {
    if (!me) {
      router.push(`/login?next=${encodeURIComponent(pathname)}`);
      return;
    }
    setBusy(true);
    try {
      onChange(state.following ? await usersApi.unfollow(username) : await usersApi.follow(username));
    } catch (error) {
      toast.error(errorMessage(error));
    } finally {
      setBusy(false);
    }
  };

  return (
    <Button variant={state.following ? "secondary" : "primary"} loading={busy} aria-pressed={state.following} onClick={toggle}>
      {state.following ? "Following" : "Follow"}
    </Button>
  );
}
