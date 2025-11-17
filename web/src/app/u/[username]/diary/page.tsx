"use client";

import { use } from "react";
import { DiaryList } from "@/components/diary/DiaryList";

export default function ProfileDiaryPage({ params }: { params: Promise<{ username: string }> }) {
  const { username } = use(params);
  return <DiaryList username={username} />;
}
