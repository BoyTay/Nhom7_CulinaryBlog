import "server-only";

import { ApiError } from "../api/client.ts";
import type { ApiProblemDetails, CategoryDto } from "../api/contracts.ts";
import { recipeServerApiBaseUrl } from "../recipes/server-api.ts";
import type { CategoryDetailDto } from "./category-view.ts";

async function getJson<T>(path: string, revalidate: number): Promise<T> {
  const response = await fetch(`${recipeServerApiBaseUrl()}${path}`, {
    headers: { Accept: "application/json" },
    next: { revalidate },
  });
  if (!response.ok) {
    const problem = (await response.json().catch(() => ({}))) as ApiProblemDetails;
    throw new ApiError(response.status, problem);
  }
  return response.json() as Promise<T>;
}

export function getCategoryList(): Promise<CategoryDto[]> {
  return getJson<CategoryDto[]>("/categories", 3600);
}

export function getCategoryDetail(slug: string, page: number): Promise<CategoryDetailDto> {
  return getJson<CategoryDetailDto>(`/categories/${encodeURIComponent(slug)}?page=${page}&pageSize=12`, 600);
}
