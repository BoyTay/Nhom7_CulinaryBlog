import { SiteHeader, Skeleton, SkeletonGroup } from "@/components/shared";

export default function RecipeDetailLoading() {
  return (
    <div className="recipe-detail-page ds-scope">
      <div className="recipe-detail-desktop-header"><SiteHeader /></div>
      <SkeletonGroup label="Đang tải công thức" className="recipe-detail-loading">
        <Skeleton className="recipe-detail-loading-image" height="27.5rem" />
        <div className="recipe-detail-loading-copy">
          <Skeleton width="70%" height="3.5rem" />
          <Skeleton width="100%" height="1.2rem" />
          <Skeleton width="88%" height="1.2rem" />
          <Skeleton width="100%" height="6rem" />
        </div>
      </SkeletonGroup>
    </div>
  );
}
