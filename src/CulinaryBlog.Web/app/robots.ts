import type { MetadataRoute } from "next";

export default function robots(): MetadataRoute.Robots {
  const origin = (process.env.NEXT_PUBLIC_SITE_URL ?? "http://localhost").replace(/\/+$/, "");

  return {
    rules: {
      userAgent: "*",
      allow: "/",
      disallow: ["/dashboard", "/profile", "/api/"],
    },
    sitemap: `${origin}/sitemap.xml`,
  };
}
