import type { RecipeDifficulty, RecipeListItemDto } from "./contracts.ts";

export const LIST_PAGE_SIZE = 12;

export type ListSort = "newest" | "name" | "cook";
export type MaxCookMinutes = 15 | 30 | 60 | 90;

export interface ListParams {
  q: string;
  difficulty?: RecipeDifficulty;
  maxCook?: MaxCookMinutes;
  sort: ListSort;
  categoryId?: string;
  page: number;
}

export interface ListView {
  items: RecipeListItemDto[];
  totalCount: number;
  totalPages: number;
  page: number;
}

type RawParams = Record<string, string | string[] | undefined>;

const difficulties = new Set<string>(["Easy", "Medium", "Hard"]);
const sorts = new Set<string>(["newest", "name", "cook"]);
const maxCooks = new Set<string>(["15", "30", "60", "90"]);
const uuidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
const MAX_QUERY_LENGTH = 100;

function single(value: string | string[] | undefined): string | undefined {
  return typeof value === "string" ? value : undefined;
}

/** Unknown or repeated values are ignored so a hand-edited URL never breaks the page. */
export function parseListParams(raw: RawParams): ListParams {
  const q = (single(raw.q) ?? "").trim().slice(0, MAX_QUERY_LENGTH);
  const difficulty = single(raw.difficulty);
  const maxCook = single(raw.maxCook);
  const sort = single(raw.sort);
  const categoryId = single(raw.categoryId);
  const page = single(raw.page);

  return {
    q,
    ...(difficulty && difficulties.has(difficulty) ? { difficulty: difficulty as RecipeDifficulty } : {}),
    ...(maxCook && maxCooks.has(maxCook) ? { maxCook: Number(maxCook) as MaxCookMinutes } : {}),
    sort: sort && sorts.has(sort) ? (sort as ListSort) : "newest",
    ...(categoryId && uuidPattern.test(categoryId) ? { categoryId } : {}),
    page: page && /^[1-9]\d*$/.test(page) ? Number(page) : 1,
  };
}

function normalize(value: string): string {
  return value
    .toLowerCase()
    .replace(/đ/g, "d")
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "");
}

export function applyListView(items: readonly RecipeListItemDto[], params: ListParams): ListView {
  const tokens = normalize(params.q).split(/\s+/).filter(Boolean);

  const filtered = items.filter((item) => {
    if (params.difficulty && item.difficulty !== params.difficulty) return false;
    if (params.categoryId && item.categoryId !== params.categoryId) return false;
    if (params.maxCook && item.cookTimeMinutes > params.maxCook) return false;
    if (tokens.length === 0) return true;
    const haystack = normalize(`${item.title} ${item.description}`);
    return tokens.every((token) => haystack.includes(token));
  });

  const sorted = [...filtered].sort((a, b) => {
    switch (params.sort) {
      case "name":
        return a.title.localeCompare(b.title, "vi");
      case "cook":
        return a.cookTimeMinutes - b.cookTimeMinutes || a.title.localeCompare(b.title, "vi");
      default:
        return b.createdAt.localeCompare(a.createdAt);
    }
  });

  const totalCount = sorted.length;
  const totalPages = Math.max(1, Math.ceil(totalCount / LIST_PAGE_SIZE));
  const page = Math.min(Math.max(params.page, 1), totalPages);
  const start = (page - 1) * LIST_PAGE_SIZE;

  return { items: sorted.slice(start, start + LIST_PAGE_SIZE), totalCount, totalPages, page };
}
