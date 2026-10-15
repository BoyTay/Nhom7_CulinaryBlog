import { SiteHeader, Skeleton, SkeletonCardGrid } from "@/components/shared";

export default function RecipesLoading() {
  return (
    <div className="recipes-page ds-scope">
      <SiteHeader />
      <main className="recipe-list-main" aria-busy="true" aria-label="Đang tải danh sách công thức">
        <header className="recipe-list-heading">
          <h1>Tất cả công thức</h1>
          <Skeleton width="7rem" height="1rem" />
        </header>
        <div className="recipe-list-filter-skeleton">
          <Skeleton width="100%" height="3.5rem" />
        </div>
        <SkeletonCardGrid
          count={8}
          label="Đang tải công thức"
          minColumnWidth="260px"
          cardHeight="15rem"
        />
      </main>
    </div>
  );
}
