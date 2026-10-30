import "server-only";

import { createRecipeClient } from "./client.ts";
import type { RecipeDetailDto, RecipeListItemDto } from "./contracts.ts";
import { pickRelated } from "./related.ts";
import { recipeServerApiBaseUrl } from "./server-api.ts";

/** Related recipes are a nicety: any failure just hides the section. */
export async function getRelatedRecipes(detail: Pick<RecipeDetailDto, "id" | "categoryId">): Promise<RecipeListItemDto[]> {
  try {
    const client = createRecipeClient({
      baseUrl: recipeServerApiBaseUrl(),
      getAccessToken: () => null,
      cache: "force-cache",
    });
    const { data } = await client.list({ categoryId: detail.categoryId, status: "Published" });
    return pickRelated(data, detail);
  } catch {
    return [];
  }
}
