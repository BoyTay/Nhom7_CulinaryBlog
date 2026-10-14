import type { Metadata } from "next";
import { Suspense } from "react";
import { Skeleton, SkeletonCardGrid } from "@/components/shared";
import SearchPage from "./search-page";
import "./search.css";

export const metadata: Metadata = { title: "Tìm công thức" };

export default function SearchRoute() {
  return (
    <Suspense fallback={<SearchLoading />}>
      <SearchPage />
    </Suspense>
  );
}

function SearchLoading() {
  return (
    <main className="search-shell ds-scope" aria-busy="true" aria-label="Đang tải tìm kiếm">
      <p className="search-kicker">Culinary Blog</p>
      <h1>Tìm công thức</h1>
      <div className="search-skeleton-bar">
        <Skeleton height="100%" />
      </div>
      <SkeletonCardGrid count={3} label="Đang tải tìm kiếm" />
    </main>
  );
}
