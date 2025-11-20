"use client";

import { DiaryList } from "@/components/diary/DiaryList";
import { OwnPage } from "@/components/layout/OwnPage";
import { LinkButton } from "@/components/ui/Button";

export default function MyDiaryPage() {
  return (
    <OwnPage title="My diary" subtitle="Everything you play, finish, shelve or want to try.">
      {(username) => <DiaryList username={username} emptyAction={<LinkButton href="/search">Find a game to log</LinkButton>} />}
    </OwnPage>
  );
}
