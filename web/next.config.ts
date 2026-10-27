import type { NextConfig } from "next";

const catalogApiUrl = process.env.CATALOG_API_URL ?? "http://localhost:5100";
const ordersApiUrl = process.env.ORDERS_API_URL ?? "http://localhost:5200";

const nextConfig: NextConfig = {
  cacheComponents: true,
  partialPrefetching: true,
  turbopack: {
    rules: {
      "*.css": {
        loaders: ["@tailwindcss/turbopack"],
        as: "*.css",
      },
    },
  },
  // The browser only ever talks to this Next.js server; these rules forward
  // /api/catalog/* and /api/orders/* to the .NET APIs, so no CORS is needed.
  async rewrites() {
    return [
      { source: "/api/catalog/:path*", destination: `${catalogApiUrl}/:path*` },
      { source: "/api/orders/:path*", destination: `${ordersApiUrl}/:path*` },
    ];
  },
};

export default nextConfig;
