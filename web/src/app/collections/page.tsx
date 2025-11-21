import type { Metadata } from "next";
import { CollectionsView } from "./CollectionsView";

export const metadata: Metadata = { title: "Collections" };

export default function CollectionsPage() {
  return <CollectionsView />;
}
