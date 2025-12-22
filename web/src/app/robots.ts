import type { MetadataRoute } from "next";

/** Public catalog and profiles may be indexed; account areas and the API may not. */
export default function robots(): MetadataRoute.Robots {
  return {
    rules: [{ userAgent: "*", allow: "/", disallow: ["/api/", "/settings", "/notifications", "/feed", "/admin"] }],
  };
}
