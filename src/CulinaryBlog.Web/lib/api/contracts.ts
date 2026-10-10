export type CategoryId = string;

export interface CategoryDto {
  id: CategoryId;
  name: string;
  slug: string;
  description: string | null;
  imageUrl: string | null;
  orderIndex: number;
  recipeCount: number;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface RecipeSearchItem {
  id: string;
  slug: string;
  title: string;
  description: string;
  categoryId: CategoryId;
  difficulty: "Easy" | "Medium" | "Hard";
  cookTime: number;
  publishedAt: string;
  relevanceScore: number | null;
}

export interface RecipeSearchOptions {
  q: string;
  categoryId: string;
  difficulty: string;
  maxCookTime: string;
  minServings: string;
  sort: string;
  page: number;
  pageSize: number;
}

export interface ApiProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errorCode?: string;
  errors?: Record<string, string[]>;
}
