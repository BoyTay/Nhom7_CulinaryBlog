import "server-only";

import type { CategoryDto } from "../api/contracts.ts";
import type { RecipeListItemDto } from "../recipes/contracts.ts";
import { recipeServerApiBaseUrl } from "../recipes/server-api.ts";
import { buildHomeViewModel, type HomeViewModel } from "./home-data.ts";

async function getJson<T>(url: string): Promise<T> {
  const response = await fetch(url, {
    headers: { Accept: "application/json" },
    next: { revalidate: 3600 },
  });
  if (!response.ok) throw new Error(`Request failed with ${response.status}`);
  return response.json() as Promise<T>;
}

export async function getHomeData(): Promise<HomeViewModel> {
  const baseUrl = recipeServerApiBaseUrl();
  const [recipes, categories] = await Promise.allSettled([
    getJson<RecipeListItemDto[]>(`${baseUrl}/recipes?status=Published`),
    getJson<CategoryDto[]>(`${baseUrl}/categories`),
  ]);

  return buildHomeViewModel(
    recipes.status === "fulfilled" ? recipes.value : [],
    categories.status === "fulfilled" ? categories.value : [],
    { recipes: recipes.status === "rejected", categories: categories.status === "rejected" },
  );
}
