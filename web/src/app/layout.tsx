import type { Metadata, Viewport } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: { default: "GameLog — your gaming journal", template: "%s · GameLog" },
  description:
    "Log the games you finish, rate and review them, build collections and see what other players think.",
};

export const viewport: Viewport = {
  themeColor: "#0a0c13",
  colorScheme: "dark",
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body className="min-h-dvh">{children}</body>
    </html>
  );
}
