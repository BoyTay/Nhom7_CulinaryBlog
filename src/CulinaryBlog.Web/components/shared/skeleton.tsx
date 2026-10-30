import type { CSSProperties, ReactNode } from "react";

type SkeletonProps = {
  width?: CSSProperties["width"];
  height?: CSSProperties["height"];
  className?: string;
};

/** Decorative placeholder block; wrap groups in SkeletonGroup so assistive tech hears one status. */
export function Skeleton({ width, height = "1rem", className }: SkeletonProps) {
  return (
    <span
      className={["ui-skeleton", className].filter(Boolean).join(" ")}
      style={{ width, height }}
      aria-hidden="true"
    />
  );
}

export function SkeletonGroup({ label, children, className, style }: { label: string; children: ReactNode; className?: string; style?: CSSProperties }) {
  return (
    <div className={className} style={style} role="status" aria-label={label} aria-busy="true">
      {children}
    </div>
  );
}

type SkeletonCardGridProps = {
  count?: number;
  label?: string;
  /** Minimum column width; use "100%" for a single column of row-shaped cards. */
  minColumnWidth?: string;
  cardHeight?: string;
};

/** Same box as a recipe card, so the page does not jump when the data arrives. */
function SkeletonRecipeCard() {
  return (
    <div className="ui-skeleton-recipe" aria-hidden="true">
      <Skeleton className="ui-skeleton-photo" />
      <div>
        <Skeleton width="75%" height="20px" />
        <Skeleton width="95%" height="12px" />
        <Skeleton width="60%" height="12px" />
        <div className="ui-skeleton-meta">
          <Skeleton width="90px" height="14px" />
          <Skeleton className="ui-skeleton-pill" width="70px" height="22px" />
        </div>
      </div>
    </div>
  );
}

/** Grid of card-shaped placeholders matching the card layout it stands in for. */
export function SkeletonCardGrid({
  count = 6,
  label = "Đang tải nội dung",
  minColumnWidth = "16rem",
  cardHeight,
}: SkeletonCardGridProps) {
  return (
    <SkeletonGroup
      label={label}
      className="ui-skeleton-grid"
      style={{ gridTemplateColumns: `repeat(auto-fit, minmax(min(${minColumnWidth}, 100%), 1fr))` }}
    >
      {Array.from({ length: count }, (_, index) => (
        cardHeight
          ? <Skeleton key={index} className="ui-skeleton-card" height={cardHeight} />
          : <SkeletonRecipeCard key={index} />
      ))}
    </SkeletonGroup>
  );
}

/** Stacked text-line placeholders for list rows. */
export function SkeletonList({ rows = 4, label = "Đang tải danh sách" }: { rows?: number; label?: string }) {
  return (
    <SkeletonGroup label={label} className="ui-skeleton-list">
      {Array.from({ length: rows }, (_, index) => (
        <div className="ui-skeleton-row" key={index}>
          <Skeleton width="55%" height="1.1rem" />
          <Skeleton width="35%" height="0.85rem" />
        </div>
      ))}
    </SkeletonGroup>
  );
}
