"use client";

import { OwnPage } from "@/components/layout/OwnPage";
import { RecapView } from "@/components/stats/RecapView";
import { useSession } from "@/stores/session";

export default function MyRecapPage() {
  const name = useSession((s) => s.user?.displayName);
  return (
    <OwnPage title="Yearly retrospective" subtitle="Your year in games, with a card to share.">
      {(username) => <RecapView username={username} displayName={name ?? username} />}
    </OwnPage>
  );
}
