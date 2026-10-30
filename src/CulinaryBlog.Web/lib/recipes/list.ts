import { categoryHref, recipeDetailHref } from "../navigation/routes.ts";
import { cookTimeLabel, difficultyLabel } from "../ui/format.ts";
import type { RecipeDifficulty, RecipeListItemDto } from "./contracts.ts";

export type RecipeListSearchParams = Record<string, string | string[] | undefined>;

export interface RecipeListCardModel {
  id: string;
  href: string;
  title: string;
  description: string;
  categoryName: string;
  categoryHref: string;
  cookTimeLabel: string;
  difficulty: RecipeDifficulty;
  difficultyLabel: string;
  thumbnailUrl: string | null;
  thumbnailAlt: string;
}

export type RecipeCardModel = RecipeListCardModel;

export function toRecipeCardModel(recipe: RecipeListItemDto): RecipeCardModel {
  return {
    id: recipe.id,
    href: recipeDetailHref(recipe),
    title: recipe.title,
    description: recipe.description,
    categoryName: recipe.categoryName,
    categoryHref: categoryHref(recipe.categorySlug),
    cookTimeLabel: cookTimeLabel(recipe.cookTimeMinutes),
    difficulty: recipe.difficulty,
    difficultyLabel: difficultyLabel(recipe.difficulty),
    thumbnailUrl: recipe.thumbnailUrl,
    thumbnailAlt: `Ảnh món ${recipe.title}`,
  };
}

export type RecipeListViewModel =
  | { state: "error"; cards: []; totalCount: 0 }
  | { state: "empty"; cards: []; totalCount: 0 }
  | { state: "populated"; cards: RecipeListCardModel[]; totalCount: number };

export function createRecipeListViewModel(
  recipes: readonly RecipeListItemDto[],
  hasError = false,
): RecipeListViewModel {
  if (hasError) return { state: "error", cards: [], totalCount: 0 };
  if (recipes.length === 0) return { state: "empty", cards: [], totalCount: 0 };

  return {
    state: "populated",
    totalCount: recipes.length,
    cards: recipes.map(toRecipeCardModel),
  };
}

export { difficultyLabel };
