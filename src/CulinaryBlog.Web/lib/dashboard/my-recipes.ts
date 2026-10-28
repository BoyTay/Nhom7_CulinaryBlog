import type { RecipeClient } from "../recipes/client.ts";
import type { RecipeListItemDto, RecipeStatus } from "../recipes/contracts.ts";

export type RecipeAction = "publish" | "unpublish" | "archive" | "restore" | "delete";
export type StatusTab = "all" | "draft" | "published" | "archived";

export interface MyRecipeSummary {
  draft: number;
  published: number;
  archived: number;
}

export function summarizeMyRecipes(items: readonly Pick<RecipeListItemDto, "status">[]): MyRecipeSummary {
  const summary: MyRecipeSummary = { draft: 0, published: 0, archived: 0 };
  for (const item of items) {
    if (item.status === "Draft") summary.draft += 1;
    else if (item.status === "Published") summary.published += 1;
    else summary.archived += 1;
  }
  return summary;
}

export function availableActions(status: RecipeStatus): RecipeAction[] {
  switch (status) {
    case "Draft":
      return ["publish", "delete"];
    case "Published":
      return ["unpublish", "archive"];
    case "Archived":
      return ["restore", "delete"];
  }
}

const genericLifecycleError = "Không thể thực hiện thao tác. Vui lòng thử lại.";

export function explainLifecycleError(problemType: string | undefined): string {
  switch (problemType?.toUpperCase()) {
    case "RECIPE_PUBLISH_INCOMPLETE":
      return "Cần ít nhất một nguyên liệu và một bước thực hiện trước khi xuất bản.";
    case "RECIPE_CONCURRENCY_CONFLICT":
      return "Công thức vừa được thay đổi ở nơi khác. Hãy tải lại danh sách rồi thử lại.";
    default:
      return genericLifecycleError;
  }
}

export const actionLabels: Record<RecipeAction, { verb: string; done: string }> = {
  publish: { verb: "Xuất bản", done: "Đã xuất bản công thức." },
  unpublish: { verb: "Hủy xuất bản", done: "Đã chuyển công thức về bản nháp." },
  archive: { verb: "Lưu trữ", done: "Đã lưu trữ công thức." },
  restore: { verb: "Khôi phục", done: "Đã khôi phục công thức về bản nháp." },
  delete: { verb: "Xóa", done: "Đã xóa công thức." },
};

type LifecycleClient = Pick<RecipeClient, "detail" | "publish" | "unpublish" | "archive" | "delete">;

/** Lifecycle endpoints require If-Match, so read the current ETag right before mutating. */
export async function runRecipeAction(client: LifecycleClient, id: string, action: RecipeAction): Promise<void> {
  const { eTag } = await client.detail(id);
  if (!eTag) throw new Error("Máy chủ không trả về ETag cho công thức này.");
  // A restore is an unpublish: the domain moves an archived recipe back to Draft.
  await client[action === "restore" ? "unpublish" : action](id, eTag);
}

const statusByTab: Record<Exclude<StatusTab, "all">, RecipeStatus> = {
  draft: "Draft",
  published: "Published",
  archived: "Archived",
};

export function parseStatusTab(raw: string | undefined): StatusTab {
  return raw === "draft" || raw === "published" || raw === "archived" ? raw : "all";
}

export function filterByStatus<T extends Pick<RecipeListItemDto, "status">>(items: readonly T[], tab: StatusTab): T[] {
  return tab === "all" ? [...items] : items.filter((item) => item.status === statusByTab[tab]);
}
