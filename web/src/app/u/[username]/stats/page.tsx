"use client";

import { use } from "react";
import { StatsView } from "@/components/stats/StatsView";

export default function ProfileStatsPage({ params }: { params: Promise<{ username: string }> }) {
  const { username } = use(params);
  return <StatsView username={username} />;
}
