import type { Metadata } from "next";
import { Suspense } from "react";
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
    <main className="search-shell" aria-busy="true" aria-label="Đang tải tìm kiếm">
      <p className="search-kicker">Culinary Blog</p>
      <h1>Tìm công thức</h1>
      <div className="search-skeleton search-skeleton-bar" />
      <div className="search-skeleton-grid">
        {Array.from({ length: 3 }, (_, index) => (
          <div className="search-skeleton search-skeleton-card" key={index} />
        ))}
      </div>
    </main>
  );
}
