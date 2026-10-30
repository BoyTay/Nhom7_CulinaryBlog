import type { RecipeStatus } from "@/lib/recipes/contracts";

const labels: Record<RecipeStatus, string> = {
  Draft: "Bản nháp",
  Published: "Đã xuất bản",
  Archived: "Lưu trữ",
};

export function RecipeStatusBadge({ status }: { status: RecipeStatus }) {
  return <span className={`badge badge-${status.toLowerCase()} dashboard-row-status`}>{labels[status]}</span>;
}
