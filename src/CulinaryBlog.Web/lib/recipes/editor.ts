import { ApiError } from "../api/client.ts";
import type { ApiProblemDetails } from "../api/contracts.ts";
import type { RecipeClient } from "./client.ts";
import type { RecipeDetailDto } from "./contracts.ts";
import {
  formToCreateRecipeRequest,
  formToUpdateRecipeRequest,
} from "./form-mapper.ts";
import type { RecipeEditorInput, RecipeEditorValues } from "./schema.ts";

export const RECIPE_EDITOR_ROLES = ["Author", "Admin"] as const;
export const RECIPE_EDITOR_STEPS = [
  "Thông tin",
  "Nguyên liệu",
  "Các bước",
  "Ảnh & dinh dưỡng",
] as const;

export type RecipeEditorStep = 1 | 2 | 3 | 4;
export type RecipeEditorFieldName =
  | "title"
  | "slug"
  | "description"
  | "categoryId"
  | "difficulty"
  | "prepTimeMinutes"
  | "cookTimeMinutes"
  | "servings"
  | "nutrition.calories"
  | "nutrition.proteinGrams"
  | "nutrition.carbohydratesGrams"
  | "nutrition.fatGrams";

export type RecipeEditorConflictKind =
  | "stale"
  | "precondition-required"
  | "invalid-etag";

export function createRecipeEditorDefaults(): RecipeEditorInput {
  return {
    title: "",
    slug: "",
    description: "",
    categoryId: "",
    difficulty: "Easy",
    prepTimeMinutes: 0,
    cookTimeMinutes: 0,
    servings: 1,
    nutrition: {
      calories: undefined,
      proteinGrams: undefined,
      carbohydratesGrams: undefined,
      fatGrams: undefined,
    },
    ingredients: [],
    steps: [],
  };
}

export function slugifyRecipeTitle(value: string): string {
  return value
    .trim()
    .toLocaleLowerCase("vi")
    .replace(/đ/g, "d")
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "")
    .slice(0, 220);
}

/** After creating a draft the editor reopens on step 4 so the author can add photos right away. */
export function initialEditorStep(created: string | undefined): RecipeEditorStep {
  return created === "1" || created === "partial" ? 4 : 1;
}

export function moveRecipeEditorStep(
  current: RecipeEditorStep,
  direction: "previous" | "next",
): RecipeEditorStep {
  const offset = direction === "next" ? 1 : -1;
  return Math.min(4, Math.max(1, current + offset)) as RecipeEditorStep;
}

export function canAccessRecipeEditor(roles: readonly string[]): boolean {
  const normalized = roles.map((role) => role.toLowerCase());
  return RECIPE_EDITOR_ROLES.some((role) => normalized.includes(role.toLowerCase()));
}

const apiFieldMap: Record<string, RecipeEditorFieldName> = {
  title: "title",
  slug: "slug",
  description: "description",
  categoryid: "categoryId",
  difficulty: "difficulty",
  preptimeminutes: "prepTimeMinutes",
  cooktimeminutes: "cookTimeMinutes",
  servings: "servings",
  calories: "nutrition.calories",
  nutritioncalories: "nutrition.calories",
  proteingrams: "nutrition.proteinGrams",
  nutritionproteingrams: "nutrition.proteinGrams",
  carbohydratesgrams: "nutrition.carbohydratesGrams",
  nutritioncarbohydratesgrams: "nutrition.carbohydratesGrams",
  fatgrams: "nutrition.fatGrams",
  nutritionfatgrams: "nutrition.fatGrams",
};

export function mapRecipeProblemFieldErrors(
  problem: ApiProblemDetails,
): Partial<Record<RecipeEditorFieldName, string>> {
  const mapped: Partial<Record<RecipeEditorFieldName, string>> = {};

  for (const [rawName, messages] of Object.entries(problem.errors ?? {})) {
    const normalized = rawName.toLowerCase().replace(/[^a-z0-9]/g, "");
    const field = apiFieldMap[normalized]
      ?? apiFieldMap[normalized.replace(/^(request|command)/, "")];
    if (field && messages.length > 0) mapped[field] = messages.join(" ");
  }

  return mapped;
}

export function recipeEditorConflictKind(error: unknown): RecipeEditorConflictKind | null {
  if (!(error instanceof ApiError)) return null;
  const code = (error.problem.type ?? error.problem.errorCode ?? "").toUpperCase();
  if (code === "RECIPE_CONCURRENCY_CONFLICT" || error.status === 412) return "stale";
  if (code === "PRECONDITION_REQUIRED" || error.status === 428) return "precondition-required";
  if (code === "INVALID_ETAG") return "invalid-etag";
  return null;
}

export function recipeEditorErrorMessage(error: unknown): string {
  if (!(error instanceof ApiError)) {
    return "Không thể kết nối máy chủ. Vui lòng thử lại.";
  }

  if (error.status === 401) return "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
  if (error.status === 403) return "Bạn không có quyền chỉnh sửa công thức này.";
  if (error.status === 404) return "Không tìm thấy công thức cần chỉnh sửa.";
  if (error.status === 422) return "Dữ liệu chưa hợp lệ. Vui lòng kiểm tra các trường được đánh dấu.";
  return error.problem.detail ?? error.problem.title ?? "Không thể lưu công thức.";
}

export function replaceRecipeEtag(_current: string | null, returned: string | null): string | null {
  return returned?.trim() ? returned : null;
}

export class MissingRecipeEtagError extends Error {
  constructor() {
    super("The recipe response did not include an ETag.");
  }
}

export class RecipeCreatedContinuationError extends Error {
  readonly recipeId: string;

  constructor(recipeId: string, cause: unknown) {
    super("The Draft was created but its continuation update did not finish.", { cause });
    this.recipeId = recipeId;
  }
}

type EditorRecipeClient = Pick<RecipeClient, "create" | "detail" | "update">;

export type SaveRecipeBaseResult =
  | { kind: "created"; id: string; detail: RecipeDetailDto; eTag: string }
  | { kind: "updated"; detail: RecipeDetailDto; eTag: string };

export async function saveRecipeBase(
  client: EditorRecipeClient,
  operation:
    | { mode: "create"; values: RecipeEditorValues }
    | { mode: "edit"; id: string; eTag: string | null; values: RecipeEditorValues },
): Promise<SaveRecipeBaseResult> {
  if (operation.mode === "create") {
    const created = await client.create(formToCreateRecipeRequest(operation.values));
    try {
      const authoritative = await client.detail(created.data.id);
      if (!authoritative.eTag) throw new MissingRecipeEtagError();
      const updated = await client.update(
        created.data.id,
        formToUpdateRecipeRequest(operation.values),
        authoritative.eTag,
      );
      if (!updated.eTag) throw new MissingRecipeEtagError();
      return {
        kind: "created",
        id: created.data.id,
        detail: updated.data,
        eTag: updated.eTag,
      };
    } catch (error) {
      throw new RecipeCreatedContinuationError(created.data.id, error);
    }
  }

  if (!operation.eTag) throw new MissingRecipeEtagError();
  const response = await client.update(
    operation.id,
    formToUpdateRecipeRequest(operation.values),
    operation.eTag,
  );
  const eTag = replaceRecipeEtag(operation.eTag, response.eTag);
  if (!eTag) throw new MissingRecipeEtagError();
  return { kind: "updated", detail: response.data, eTag };
}

export interface RecipeReadinessItem {
  label: string;
  done: boolean;
  /** Publishing is blocked until every required item is done; the photo is only a recommendation. */
  required: boolean;
}

export function recipeReadiness(
  values: {
    title?: string;
    description?: string;
    categoryId?: string;
    ingredients?: readonly unknown[];
    steps?: readonly unknown[];
  },
  imageCount: number,
): RecipeReadinessItem[] {
  return [
    { label: "Có tiêu đề và mô tả", done: Boolean(values.title?.trim() && values.description?.trim()), required: true },
    { label: "Đã chọn danh mục", done: Boolean(values.categoryId), required: true },
    { label: "Có ít nhất 1 nguyên liệu", done: (values.ingredients?.length ?? 0) > 0, required: true },
    { label: "Có ít nhất 1 bước thực hiện", done: (values.steps?.length ?? 0) > 0, required: true },
    { label: "Có ảnh đại diện", done: imageCount > 0, required: false },
  ];
}
