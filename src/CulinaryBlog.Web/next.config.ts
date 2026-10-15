import type { NextConfig } from "next";
import { createRecipeImageRemotePatterns } from "./lib/recipes/image-config";

const nextConfig: NextConfig = {
  output: "standalone",
  poweredByHeader: false,
  reactStrictMode: true,
  images: {
    remotePatterns: createRecipeImageRemotePatterns({
      minioEndpoint: process.env.MINIO_PUBLIC_ENDPOINT ?? "http://localhost:9000",
      cdnEndpoint: process.env.RECIPE_IMAGE_CDN_URL,
    }),
  },
  async rewrites() {
    return [{ source: "/api/v1/:path*", destination: `${process.env.API_PROXY_TARGET ?? "http://localhost:8080"}/api/v1/:path*` }];
  },
};

export default nextConfig;
