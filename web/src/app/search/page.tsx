import type { Metadata } from "next";
import { Suspense } from "react";
import { SearchView } from "./SearchView";

export const metadata: Metadata = { title: "Browse games" };

export default function SearchPage() {
  return (
    <Suspense>
      <SearchView />
    </Suspense>
  );
}
