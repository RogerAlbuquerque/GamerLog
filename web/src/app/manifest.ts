import type { MetadataRoute } from "next";

export default function manifest(): MetadataRoute.Manifest {
  return {
    name: "GameLog",
    short_name: "GameLog",
    description: "Your gaming journal: log, rate, review and look back on every game you play.",
    start_url: "/",
    display: "standalone",
    background_color: "#0a0c13",
    theme_color: "#0a0c13",
    icons: [{ src: "/icon.svg", sizes: "any", type: "image/svg+xml" }],
  };
}
