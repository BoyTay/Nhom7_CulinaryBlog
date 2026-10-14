export type CategoryId = string;
export type RecipeId = string;
export type UserId = string;

// --- Phân hệ Chung (Common & System) ---
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface ApiProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errorCode?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}

// --- Phân hệ Danh mục (Category) ---
export interface CategoryDto {
  id: CategoryId;
  name: string;
  slug: string;
  description: string | null;
  imageUrl: string | null;
  orderIndex: number;
  recipeCount: number;
}

// --- Phân hệ Xác thực & Quản lý người dùng (FR-AUTH) ---
export interface AuthUserDto {
  id: UserId;
  email: string;
  displayName: string;
  avatarUrl: string | null;
  bio: string | null;
  roles: string[];
}

export interface AuthResponseDto {
  accessToken: string;
  expiresIn: number;
  user: AuthUserDto;
}

export interface RegisterRequest {
  displayName: string;
  email: string;
  password: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface GoogleLoginRequest {
  idToken: string;
}

export interface UpdateProfileRequest {
  displayName?: string | null;
  avatarUrl?: string | null;
  bio?: string | null;
}

// --- Phân hệ Tìm kiếm công thức (FR-SRCH) ---
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

// --- Phân hệ Công thức nấu ăn (FR-RCP) ---
export type RecipeDifficulty = "Easy" | "Medium" | "Hard";
export type RecipeStatus = "Draft" | "Published" | "Archived";

export interface RecipeIngredientDto {
  id: string;
  name: string;
  amount: string;
  unit: string;
  orderIndex: number;
}

export interface RecipeStepDto {
  id: string;
  stepNumber: number;
  instruction: string;
  imageUrl: string | null;
}

export interface RecipeImageDto {
  id: string;
  imageUrl: string;
  isPrimary: boolean;
  orderIndex: number;
}

export interface RecipeDetailDto {
  id: RecipeId;
  title: string;
  slug: string;
  description: string;
  categoryId: CategoryId;
  categoryName: string;
  difficulty: RecipeDifficulty;
  prepTime: number;
  cookTime: number;
  servings: number;
  status: RecipeStatus;
  viewCount: number;
  likeCount: number;
  publishedAt: string | null;
  createdAt: string;
  updatedAt: string | null;
  authorId: UserId;
  authorName: string;
  primaryImageUrl: string | null;
  ingredients: RecipeIngredientDto[];
  steps: RecipeStepDto[];
  images: RecipeImageDto[];
  eTag: string;
}

export interface CreateRecipeRequest {
  title: string;
  description: string;
  categoryId: CategoryId;
  difficulty: RecipeDifficulty;
  prepTime: number;
  cookTime: number;
  servings: number;
}

export interface UpdateRecipeRequest {
  title: string;
  description: string;
  categoryId: CategoryId;
  difficulty: RecipeDifficulty;
  prepTime: number;
  cookTime: number;
  servings: number;
}
