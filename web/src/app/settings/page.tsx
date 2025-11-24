"use client";

import { OwnPage } from "@/components/layout/OwnPage";
import { ErrorState } from "@/components/ui/States";
import { Skeleton } from "@/components/ui/Skeleton";
import { useAsync } from "@/hooks/useAsync";
import { usersApi } from "@/lib/api/endpoints";
import { useSession } from "@/stores/session";
import { AccountSettings } from "./AccountSettings";
import { ProfileSettings } from "./ProfileSettings";

function Settings({ username }: { username: string }) {
  const user = useSession((s) => s.user)!;
  const { data, error, reload } = useAsync(() => usersApi.profile(username), [username]);
  if (error != null) return <ErrorState error={error} onRetry={reload} />;
  if (!data) return <Skeleton className="h-96" />;
  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <ProfileSettings key={user.id} user={user} profile={data} />
      <AccountSettings />
    </div>
  );
}

export default function SettingsPage() {
  return <OwnPage title="Settings" subtitle="Your profile, avatar and account security.">{(username) => <Settings username={username} />}</OwnPage>;
}
