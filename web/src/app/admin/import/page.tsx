import type { Metadata } from "next";
import { ImportView } from "./ImportView";

export const metadata: Metadata = { title: "Import games — GameLog" };

export default function ImportPage() {
  return <ImportView />;
}
