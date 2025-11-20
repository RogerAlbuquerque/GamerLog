"use client";

import { useRef, useState } from "react";
import { Button } from "@/components/ui/Button";
import type { YearRecap } from "@/lib/api/types";
import { formatPlaytime } from "@/lib/format";
import { toast } from "@/stores/toast";

const MONTHS = ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];

const truncate = (text: string, max: number) => (text.length > max ? `${text.slice(0, max - 1)}…` : text);

/**
 * A 1080×1350 recap card drawn as SVG (no external images, so it can be exported to PNG without tainting the canvas).
 * Colours are literals because a standalone SVG cannot see the page's CSS variables.
 */
export function ShareCardSvg({ recap, displayName }: { recap: YearRecap; displayName: string }) {
  const genres = recap.topGenres.slice(0, 3);
  const top = recap.highestRated.slice(0, 3);
  return (
    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1080 1350" width="1080" height="1350" role="img" aria-label={`${displayName}'s ${recap.year} in games`}>
      <defs>
        <linearGradient id="bg" x1="0" y1="0" x2="1" y2="1">
          <stop offset="0" stopColor="#1b1030" />
          <stop offset="1" stopColor="#090b16" />
        </linearGradient>
        <linearGradient id="hl" x1="0" y1="0" x2="1" y2="0">
          <stop offset="0" stopColor="#a78bfa" />
          <stop offset="1" stopColor="#f0abfc" />
        </linearGradient>
      </defs>
      <rect width="1080" height="1350" fill="url(#bg)" />
      <circle cx="940" cy="170" r="260" fill="#7c3aed" opacity="0.18" />
      <g fontFamily="system-ui, sans-serif" fill="#f4f4fb">
        <text x="80" y="120" fontSize="34" fontWeight="700" letterSpacing="6" fill="#a78bfa">GAMELOG · YEAR IN GAMES</text>
        <text x="80" y="300" fontSize="190" fontWeight="800" fill="url(#hl)">{recap.year}</text>
        <text x="80" y="360" fontSize="40" fill="#b8b8d0">{truncate(displayName, 28)}</text>

        <text x="80" y="520" fontSize="150" fontWeight="800">{recap.gamesCompleted}</text>
        <text x="80" y="575" fontSize="38" fill="#b8b8d0">{recap.gamesCompleted === 1 ? "game completed" : "games completed"}</text>
        <text x="600" y="520" fontSize="110" fontWeight="800">{formatPlaytime(recap.totalPlaytimeMinutes)}</text>
        <text x="600" y="575" fontSize="38" fill="#b8b8d0">played</text>

        {recap.busiestMonth && (
          <text x="80" y="680" fontSize="38" fill="#b8b8d0">
            Busiest month: <tspan fill="#f4f4fb" fontWeight="700">{MONTHS[recap.busiestMonth - 1]}</tspan>
          </text>
        )}
        {genres.length > 0 && (
          <text x="80" y="740" fontSize="38" fill="#b8b8d0">
            Top genres: <tspan fill="#f4f4fb" fontWeight="700">{truncate(genres.map((g) => g.name).join(" · "), 34)}</tspan>
          </text>
        )}

        <text x="80" y="880" fontSize="34" fontWeight="700" letterSpacing="4" fill="#a78bfa">TOP RATED</text>
        {top.length === 0 && <text x="80" y="950" fontSize="38" fill="#b8b8d0">No rated games this year.</text>}
        {top.map((g, i) => (
          <g key={g.gameId}>
            <text x="80" y={960 + i * 100} fontSize="46" fontWeight="700" fill="#f0abfc">{i + 1}</text>
            <text x="150" y={960 + i * 100} fontSize="46" fontWeight="600">{truncate(g.title, 26)}</text>
            <text x="1000" y={960 + i * 100} fontSize="42" textAnchor="end" fill="#fbbf24">{g.halfStars != null ? `${g.halfStars / 2}★` : ""}</text>
          </g>
        ))}
        <text x="80" y="1290" fontSize="30" fill="#6f6f90">{recap.reviewsWritten} reviews · {recap.ratingsGiven} ratings</text>
      </g>
    </svg>
  );
}

/** Renders the SVG to a PNG blob in the browser. */
export async function svgToPng(svg: SVGSVGElement, scale = 1): Promise<Blob> {
  const xml = new XMLSerializer().serializeToString(svg);
  const url = URL.createObjectURL(new Blob([xml], { type: "image/svg+xml;charset=utf-8" }));
  try {
    const img = new Image();
    img.decoding = "async";
    await new Promise<void>((resolve, reject) => {
      img.onload = () => resolve();
      img.onerror = () => reject(new Error("Could not render the card."));
      img.src = url;
    });
    const canvas = document.createElement("canvas");
    canvas.width = 1080 * scale;
    canvas.height = 1350 * scale;
    canvas.getContext("2d")!.drawImage(img, 0, 0, canvas.width, canvas.height);
    return await new Promise<Blob>((resolve, reject) => canvas.toBlob((b) => (b ? resolve(b) : reject(new Error("Could not export the card."))), "image/png"));
  } finally {
    URL.revokeObjectURL(url);
  }
}

export function ShareCard({ recap, displayName }: { recap: YearRecap; displayName: string }) {
  const ref = useRef<HTMLDivElement>(null);
  const [busy, setBusy] = useState(false);

  const download = async () => {
    const svg = ref.current?.querySelector("svg");
    if (!svg) return;
    setBusy(true);
    try {
      const blob = await svgToPng(svg);
      const file = new File([blob], `gamelog-${recap.year}.png`, { type: "image/png" });
      if (navigator.canShare?.({ files: [file] })) {
        await navigator.share({ files: [file], title: `My ${recap.year} in games` });
      } else {
        const a = document.createElement("a");
        a.href = URL.createObjectURL(blob);
        a.download = file.name;
        a.click();
        setTimeout(() => URL.revokeObjectURL(a.href), 2000);
      }
    } catch (error) {
      if ((error as Error).name !== "AbortError") toast.error("Could not create the image. Try again.");
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="space-y-3">
      <div ref={ref} className="mx-auto w-full max-w-sm overflow-hidden rounded-2xl border border-line shadow-2xl shadow-accent/20 [&>svg]:h-auto [&>svg]:w-full">
        <ShareCardSvg recap={recap} displayName={displayName} />
      </div>
      <div className="text-center">
        <Button onClick={download} loading={busy}>Save or share as image</Button>
      </div>
    </div>
  );
}
