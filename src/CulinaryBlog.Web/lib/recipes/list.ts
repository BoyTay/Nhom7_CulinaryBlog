import { categoryHref, recipeDetailHref } from "../navigation/routes.ts";
import { cookTimeLabel, difficultyLabel } from "../ui/format.ts";
import type {
  RecipeDifficulty,
  RecipeListFilters,
  RecipeListItemDto,
} from "./contracts.ts";

export const RECIPE_LIST_PAGE_SIZE = 12;

const difficulties = new Set<RecipeDifficulty>(["Easy", "Medium", "Hard"]);
const uuidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

export type RecipeListSearchParams = Record<string, string | string[] | undefined>;

export interface ParsedRecipeListFilters {
  categoryId?: string;
  difficulty?: RecipeDifficulty;
  page: number;
}

export type RecipeListFilterResult =
  | { success: true; value: ParsedRecipeListFilters }
  | { success: false; message: string };

export interface RecipeListCardModel {
  id: string;
  href: string;
  title: string;
  categoryName: string;
  categoryHref: string;
  cookTimeLabel: string;
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
    categoryName: recipe.categoryName,
    categoryHref: categoryHref(recipe.categorySlug),
    cookTimeLabel: cookTimeLabel(recipe.cookTimeMinutes),
    difficultyLabel: difficultyLabel(recipe.difficulty),
    thumbnailUrl: recipe.thumbnailUrl,
    thumbnailAlt: `Ảnh món ${recipe.title}`,
  };
}

export type RecipeListViewModel =
  | { state: "error"; cards: []; totalCount: 0 }
  | { state: "empty"; cards: []; totalCount: 0 }
  | { state: "populated"; cards: RecipeListCardModel[]; totalCount: number };

function singleValue(value: string | string[] | undefined): string | undefined {
  return typeof value === "string" ? value : undefined;
}

export function parseRecipeListFilters(params: RecipeListSearchParams): RecipeListFilterResult {
  const categoryId = singleValue(params.categoryId)?.trim();
  const rawDifficulty = singleValue(params.difficulty)?.trim();
  const rawPage = singleValue(params.page)?.trim();
  const rawStatus = singleValue(params.status)?.trim();

  if (params.categoryId instanceof Array || (categoryId && !uuidPattern.test(categoryId))) {
    return { success: false, message: "Danh mục đã chọn không hợp lệ." };
  }

  if (
    params.difficulty instanceof Array
    || (rawDifficulty && !difficulties.has(rawDifficulty as RecipeDifficulty))
  ) {
    return { success: false, message: "Độ khó đã chọn không hợp lệ." };
  }

  if (params.page instanceof Array || (rawPage && !/^[1-9]\d*$/.test(rawPage))) {
    return { success: false, message: "Số trang phải là một số nguyên dương." };
  }

  // The public list is intentionally Published-only. Do not allow a URL to request private states.
  if (params.status instanceof Array || (rawStatus && rawStatus !== "Published")) {
    return { success: false, message: "Trạng thái này không có trên danh sách công khai." };
  }

  return {
    success: true,
    value: {
      categoryId: categoryId || undefined,
      difficulty: rawDifficulty as RecipeDifficulty | undefined,
      page: rawPage ? Number(rawPage) : 1,
    },
  };
}

export function toPublicRecipeFilters(filters: ParsedRecipeListFilters): RecipeListFilters {
  return {
    categoryId: filters.categoryId,
    difficulty: filters.difficulty,
    status: "Published",
  };
}

export interface LocalPage<T> {
  items: T[];
  page: number;
  totalPages: number;
  totalCount: number;
}

export function paginateLocally<T>(
  items: readonly T[],
  requestedPage: number,
  pageSize = RECIPE_LIST_PAGE_SIZE,
): LocalPage<T> {
  if (!Number.isInteger(pageSize) || pageSize < 1) {
    throw new RangeError("pageSize must be a positive integer.");
  }

  const totalCount = items.length;
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const page = Math.min(Math.max(Number.isInteger(requestedPage) ? requestedPage : 1, 1), totalPages);
  const start = (page - 1) * pageSize;

  return {
    items: items.slice(start, start + pageSize),
    page,
    totalPages,
    totalCount,
  };
}

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
