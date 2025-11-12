import type { Metadata } from "next";
import { ReviewView } from "./ReviewView";

export const metadata: Metadata = { title: "Review" };

export default async function ReviewPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return <ReviewView id={id} />;
}
