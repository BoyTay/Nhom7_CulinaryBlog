import type { RecipeStatus } from "@/lib/recipes/contracts";

const labels: Record<RecipeStatus, string> = {
  Draft: "Bản nháp",
  Published: "Đã xuất bản",
  Archived: "Đã lưu trữ",
};

export function RecipeStatusBadge({ status }: { status: RecipeStatus }) {
  return <span className={`badge badge-${status.toLowerCase()}`}>{labels[status]}</span>;
}
