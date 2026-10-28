import type { CategoryDto } from "../api/contracts.ts";
import type { RecipeListItemDto } from "../recipes/contracts.ts";
import { createRecipeListViewModel, type RecipeListCardModel } from "../recipes/list.ts";
import { categoryHref } from "../navigation/routes.ts";

export const HOME_FEATURED_COUNT = 6;

export interface HomeCategoryModel {
  name: string;
  href: string;
  recipeCount: number;
}

export interface HomeViewModel {
  featured: RecipeListCardModel[];
  categories: HomeCategoryModel[];
  state: "ok" | "partial" | "error";
}

export interface HomeFailures {
  recipes: boolean;
  categories: boolean;
}

export function buildHomeViewModel(
  recipes: readonly RecipeListItemDto[],
  categories: readonly CategoryDto[],
  failures: HomeFailures,
): HomeViewModel {
  const newestPublished = recipes
    .filter((recipe) => recipe.status === "Published")
    .sort((a, b) => b.createdAt.localeCompare(a.createdAt))
    .slice(0, HOME_FEATURED_COUNT);

  return {
    featured: createRecipeListViewModel(newestPublished).cards,
    categories: categories
      .filter((category) => category.recipeCount > 0)
      .map((category) => ({
        name: category.name,
        href: categoryHref(category.slug),
        recipeCount: category.recipeCount,
      })),
    state: failures.recipes && failures.categories ? "error" : failures.recipes || failures.categories ? "partial" : "ok",
  };
}
