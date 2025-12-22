import type { Metadata } from "next";
import { ImportView } from "./ImportView";

export const metadata: Metadata = { title: "Import games", robots: { index: false } };

export default function ImportPage() {
  return <ImportView />;
}
