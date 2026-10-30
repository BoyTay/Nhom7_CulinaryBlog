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
    <main aria-busy="true" aria-label="Đang tải tìm kiếm">
      <section className="search-hero">
        <div className="search-hero-inner">
          <h1>Tìm công thức</h1>
          <div className="search-skeleton-bar"><Skeleton height="100%" /></div>
        </div>
      </section>
      <div className="search-layout">
        <div className="search-column"><SkeletonCardGrid cardHeight="8.5rem" count={3} label="Đang tải tìm kiếm" minColumnWidth="100%" /></div>
      </div>
    </main>
  );
}
