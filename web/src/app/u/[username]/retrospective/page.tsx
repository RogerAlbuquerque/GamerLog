"use client";

import { use } from "react";
import { RecapView } from "@/components/stats/RecapView";

export default function ProfileRecapPage({ params }: { params: Promise<{ username: string }> }) {
  const { username } = use(params);
  return <RecapView username={username} displayName={username} />;
}
