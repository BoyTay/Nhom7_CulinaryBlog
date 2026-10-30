import type { CategoryDto, PagedResult } from "../api/contracts.ts";
import type { RecipeDifficulty } from "../recipes/contracts.ts";
import { categoryHref, recipeDetailHref } from "../navigation/routes.ts";
import type { RecipeCardModel } from "../recipes/list.ts";
import { cookTimeLabel, difficultyLabel } from "../ui/format.ts";

export interface CategoryRecipeSummaryDto {
  id: string;
  title: string;
  slug: string;
  description: string;
  createdAt: string;
  cookTimeMinutes: number;
  difficulty: RecipeDifficulty;
  thumbnailUrl: string | null;
}

export interface CategoryDetailDto {
  category: CategoryDto;
  recipes: PagedResult<CategoryRecipeSummaryDto>;
}

export interface CategoryDetailView {
  state: "empty" | "populated";
  title: string;
  subtitle: string;
  cards: RecipeCardModel[];
  page: number;
  totalPages: number;
  totalCount: number;
}

export function parseCategoryPage(raw: string | undefined): number {
  return raw && /^[1-9]\d*$/.test(raw) ? Number(raw) : 1;
}

export function buildCategoryDetailView(
  category: CategoryDto,
  recipes: PagedResult<CategoryRecipeSummaryDto>,
): CategoryDetailView {
  return {
    state: recipes.items.length === 0 ? "empty" : "populated",
    title: category.name,
    subtitle: category.description?.trim() || `${recipes.totalCount} công thức`,
    cards: recipes.items.map((recipe) => ({
      id: recipe.id,
      href: recipeDetailHref(recipe),
      title: recipe.title,
      description: recipe.description,
      categoryName: category.name,
      categoryHref: categoryHref(category.slug),
      cookTimeLabel: cookTimeLabel(recipe.cookTimeMinutes),
      difficulty: recipe.difficulty,
      difficultyLabel: difficultyLabel(recipe.difficulty),
      thumbnailUrl: recipe.thumbnailUrl,
      thumbnailAlt: `Ảnh món ${recipe.title}`,
    })),
    page: recipes.page,
    totalPages: recipes.totalPages,
    totalCount: recipes.totalCount,
  };
}
