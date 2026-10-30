import type { RecipeListItemDto } from "./contracts.ts";

/** Other published recipes from the same category, newest first. */
export function pickRelated(
  all: readonly RecipeListItemDto[],
  current: { id: string; categoryId: string },
  limit = 3,
): RecipeListItemDto[] {
  return all
    .filter((item) => item.status === "Published" && item.categoryId === current.categoryId && item.id !== current.id)
    .sort((a, b) => b.createdAt.localeCompare(a.createdAt))
    .slice(0, limit);
}
