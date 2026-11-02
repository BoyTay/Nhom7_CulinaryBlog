import "server-only";

import type { CategoryDto } from "../api/contracts.ts";
import type { RecipeListItemDto } from "../recipes/contracts.ts";
import { recipeServerApiBaseUrl } from "../recipes/server-api.ts";
import { buildHomeViewModel, type HomeViewModel } from "./home-data.ts";

// The SRS route table asks for revalidate=3600 on "/": the home page data is refreshed at most once an hour. The page itself stays rendered per request
// (see app/page.tsx), so a failed fetch is never baked into a prerendered page; only successful responses are
// kept in the Next data cache.
const HOME_REVALIDATE_SECONDS = 3600;

async function getJson<T>(url: string): Promise<T> {
  const response = await fetch(url, {
    headers: { Accept: "application/json" },
    next: { revalidate: HOME_REVALIDATE_SECONDS },
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
