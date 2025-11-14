import type { ReactNode } from "react";
import { ProfileShell } from "@/components/profile/ProfileShell";

export default async function ProfileLayout({ children, params }: { children: ReactNode; params: Promise<{ username: string }> }) {
  const { username } = await params;
  return <ProfileShell username={username}>{children}</ProfileShell>;
}
