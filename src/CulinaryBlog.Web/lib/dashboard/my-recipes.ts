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
      return ["publish", "archive", "delete"];
    case "Published":
      return ["unpublish", "archive", "delete"];
    case "Archived":
      return ["restore", "delete"];
  }
}

const genericLifecycleError = "Không thể thực hiện thao tác. Vui lòng thử lại.";

export function explainLifecycleError(problemType: string | undefined, title?: string): string {
  switch (problemType?.toUpperCase()) {
    case "RECIPE_PUBLISH_INCOMPLETE":
      return title
        ? `Chưa thể xuất bản “${title}”. Công thức cần ít nhất 1 nguyên liệu và 1 bước thực hiện.`
        : "Cần ít nhất một nguyên liệu và một bước thực hiện trước khi xuất bản.";
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

export const actionIcons = {
  archive: "M3 5h18v4H3zM5 9v10h14V9M10 13h4",
  delete: "M4 7h16M10 11v6M14 11v6M6 7l1 13h10l1-13M9 7V4h6v3",
  unpublish: "M12 5v10M8 11l4 4 4-4M5 19h14",
} as const;

export interface ActionConfirmation {
  title: string;
  description: string;
  confirmLabel: string;
  icon: string;
  tone: "danger" | "rose" | "gold";
  destructive: boolean;
}

/** The actions that ask first; publishing and restoring run straight away. */
export function actionConfirmation(action: RecipeAction, title: string): ActionConfirmation | null {
  switch (action) {
    case "delete":
      return {
        title: `Xóa “${title}”?`,
        description: "Công thức và ảnh đi kèm sẽ bị xóa khỏi tài khoản của bạn. Bạn không thể hoàn tác thao tác này.",
        confirmLabel: "Xóa công thức",
        icon: actionIcons.delete,
        tone: "danger",
        destructive: true,
      };
    case "archive":
      return {
        title: `Lưu trữ “${title}”?`,
        description: "Công thức sẽ ẩn khỏi trang công khai nhưng vẫn còn trong tài khoản. Bạn có thể khôi phục bất cứ lúc nào.",
        confirmLabel: "Lưu trữ",
        icon: actionIcons.archive,
        tone: "rose",
        destructive: false,
      };
    case "unpublish":
      return {
        title: `Hủy xuất bản “${title}”?`,
        description: "Công thức sẽ chuyển về bản nháp và người đọc không còn thấy nó nữa. Đường dẫn cũ sẽ báo không tìm thấy.",
        confirmLabel: "Hủy xuất bản",
        icon: actionIcons.unpublish,
        tone: "gold",
        destructive: false,
      };
    default:
      return null;
  }
}

/** What a draft still lacks before it can be published, or null when it has both parts. */
export function missingParts(detail: { ingredients: readonly unknown[]; steps: readonly unknown[] }): string | null {
  const noIngredients = detail.ingredients.length === 0;
  const noSteps = detail.steps.length === 0;
  if (noIngredients && noSteps) return "nguyên liệu và các bước";
  if (noIngredients) return "nguyên liệu";
  return noSteps ? "các bước" : null;
}

export function countMissingIngredients(gaps: ReadonlyMap<string, string | null>): number {
  return [...gaps.values()].filter((gap) => gap?.startsWith("nguyên liệu")).length;
}

/** The line under a draft in "Cần hoàn thiện"; null when nothing is left to do. `gap` is undefined until it has loaded. */
export function draftNote(gap: string | null | undefined, hasImage: boolean): string | null {
  if (gap === undefined) return "Bản nháp chưa xuất bản";
  if (gap) return `Còn thiếu ${gap}`;
  return hasImage ? null : "Chưa có ảnh đại diện";
}

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
