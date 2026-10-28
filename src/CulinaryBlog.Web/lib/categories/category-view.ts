import type { CategoryDto, PagedResult } from "../api/contracts.ts";
import { recipeDetailHref } from "../navigation/routes.ts";

export interface CategoryRecipeSummaryDto {
  id: string;
  title: string;
  slug: string;
  description: string;
  createdAt: string;
}

export interface CategoryDetailDto {
  category: CategoryDto;
  recipes: PagedResult<CategoryRecipeSummaryDto>;
}

export interface CategoryCardModel {
  id: string;
  title: string;
  description: string;
  href: string;
}

export interface CategoryDetailView {
  state: "empty" | "populated";
  title: string;
  description: string | null;
  cards: CategoryCardModel[];
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
    description: category.description,
    cards: recipes.items.map((recipe) => ({
      id: recipe.id,
      title: recipe.title,
      description: recipe.description,
      href: recipeDetailHref(recipe),
    })),
    page: recipes.page,
    totalPages: recipes.totalPages,
    totalCount: recipes.totalCount,
  };
}
