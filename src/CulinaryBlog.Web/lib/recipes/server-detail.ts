import "server-only";

import { createRecipeClient } from "./client.ts";
import type { RecipeDetailDto } from "./contracts.ts";
import { recipeServerApiBaseUrl } from "./server-api.ts";

function publicClient() {
  return createRecipeClient({
    baseUrl: recipeServerApiBaseUrl(),
    getAccessToken: () => null,
    cache: "force-cache",
  });
}

export async function getPublicRecipeDetail(id: string): Promise<RecipeDetailDto> {
  return (await publicClient().detail(id)).data;
}

export async function getPublicRecipeDetailBySlug(slug: string): Promise<RecipeDetailDto> {
  return (await publicClient().detailBySlug(slug)).data;
}
