export type RecipeDifficulty = "Easy" | "Medium" | "Hard";
export type RecipeStatus = "Draft" | "Published" | "Archived";

export interface RecipeListItemDto {
  id: string;
  title: string;
  slug: string;
  description: string;
  categoryId: string;
  authorId: string;
  prepTimeMinutes: number;
  cookTimeMinutes: number;
  servings: number;
  difficulty: RecipeDifficulty;
  status: RecipeStatus;
  createdAt: string;
}

export interface RecipeNutritionDto {
  calories: number;
  proteinGrams: number;
  carbohydratesGrams: number;
  fatGrams: number;
}

export interface RecipeIngredientDto {
  id: string;
  name: string;
  quantity: number;
  unit: string;
  notes: string | null;
  sortOrder: number;
}

export interface RecipeStepDto {
  id: string;
  stepNumber: number;
  description: string;
  timerMinutes: number | null;
  imageUrl: string | null;
}

export interface RecipeImageDto {
  id: string;
  url: string;
  originalUrl: string;
  mediumUrl: string | null;
  thumbnailUrl: string | null;
  altText: string | null;
  isPrimary: boolean;
  orderIndex: number;
}

export interface RecipeDetailDto {
  id: string;
  title: string;
  slug: string;
  description: string;
  categoryId: string;
  authorId: string;
  prepTimeMinutes: number;
  cookTimeMinutes: number;
  servings: number;
  difficulty: RecipeDifficulty;
  status: RecipeStatus;
  publishedAt: string | null;
  version: number;
  nutrition: RecipeNutritionDto | null;
  steps: RecipeStepDto[];
  ingredients: RecipeIngredientDto[];
  images: RecipeImageDto[];
  createdAt: string;
}

export interface CreateRecipeRequest {
  title: string;
  slug: string;
  description: string;
  categoryId: string;
  prepTimeMinutes: number;
  cookTimeMinutes: number;
  servings: number;
  difficulty: RecipeDifficulty;
}

export interface UpdateRecipeRequest extends CreateRecipeRequest {
  calories: number | null;
  proteinGrams: number | null;
  carbohydratesGrams: number | null;
  fatGrams: number | null;
}

export interface RecipeIngredientRequest {
  name: string;
  quantity: number;
  unit: string;
  notes: string | null;
}

export interface RecipeStepRequest {
  description: string;
  timerMinutes: number | null;
  imageUrl: string | null;
}

export interface CreatedResource {
  id: string;
}

export interface RecipeListFilters {
  categoryId?: string;
  difficulty?: RecipeDifficulty;
  status?: RecipeStatus;
}

/** A parsed API response together with the concurrency token returned in its ETag header. */
export interface RecipeApiResponse<T> {
  data: T;
  eTag: string | null;
}
