"use client";

import { OwnPage } from "@/components/layout/OwnPage";
import { StatsView } from "@/components/stats/StatsView";

export default function MyStatsPage() {
  return <OwnPage title="Your statistics" subtitle="Playtime, ratings and favourites across your whole diary.">{(username) => <StatsView username={username} />}</OwnPage>;
}
