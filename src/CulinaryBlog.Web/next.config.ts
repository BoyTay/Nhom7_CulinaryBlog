import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  output: "standalone",
  poweredByHeader: false,
  reactStrictMode: true,
  async rewrites() {
    return [{ source: "/api/v1/:path*", destination: `${process.env.API_PROXY_TARGET ?? "http://localhost:8080"}/api/v1/:path*` }];
  },
};

export default nextConfig;
