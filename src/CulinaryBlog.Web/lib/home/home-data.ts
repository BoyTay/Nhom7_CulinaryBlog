import type { CategoryDto } from "../api/contracts.ts";
import type { RecipeListItemDto } from "../recipes/contracts.ts";
import { toRecipeCardModel, type RecipeCardModel } from "../recipes/list.ts";
import { categoryHref } from "../navigation/routes.ts";

export const HOME_FEATURED_COUNT = 8;

export interface HomeCategoryModel {
  name: string;
  href: string;
  recipeCount: number;
}

export interface HomeViewModel {
  hero: RecipeCardModel | null;
  featured: RecipeCardModel[];
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
    .sort((a, b) => b.createdAt.localeCompare(a.createdAt));
  const hero = newestPublished.find((recipe) => recipe.thumbnailUrl) ?? null;

  return {
    hero: hero ? toRecipeCardModel(hero) : null,
    featured: newestPublished
      .filter((recipe) => recipe !== hero)
      .slice(0, HOME_FEATURED_COUNT)
      .map(toRecipeCardModel),
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
