import type {
  CreateRecipeRequest,
  RecipeDetailDto,
  RecipeIngredientRequest,
  RecipeStepRequest,
  UpdateRecipeRequest,
} from "./contracts.ts";
import type {
  RecipeEditorValues,
  RecipeIngredientValues,
  RecipeStepValues,
} from "./schema.ts";

function nullableText(value: string | undefined): string | null {
  const trimmed = value?.trim();
  return trimmed ? trimmed : null;
}

export function recipeDetailToForm(detail: RecipeDetailDto): RecipeEditorValues {
  return {
    title: detail.title,
    slug: detail.slug,
    description: detail.description,
    categoryId: detail.categoryId,
    difficulty: detail.difficulty,
    prepTimeMinutes: detail.prepTimeMinutes,
    cookTimeMinutes: detail.cookTimeMinutes,
    servings: detail.servings,
    nutrition: detail.nutrition
      ? {
          calories: detail.nutrition.calories,
          proteinGrams: detail.nutrition.proteinGrams,
          carbohydratesGrams: detail.nutrition.carbohydratesGrams,
          fatGrams: detail.nutrition.fatGrams,
        }
      : undefined,
    ingredients: [...detail.ingredients]
      .sort((left, right) => left.sortOrder - right.sortOrder)
      .map((ingredient) => ({
        id: ingredient.id,
        name: ingredient.name,
        quantity: ingredient.quantity,
        unit: ingredient.unit,
        notes: ingredient.notes ?? undefined,
      })),
    steps: [...detail.steps]
      .sort((left, right) => left.stepNumber - right.stepNumber)
      .map((step) => ({
        id: step.id,
        description: step.description,
        timerMinutes: step.timerMinutes ?? undefined,
        imageUrl: step.imageUrl ?? undefined,
      })),
  };
}

export function formToCreateRecipeRequest(form: RecipeEditorValues): CreateRecipeRequest {
  return {
    title: form.title.trim(),
    slug: form.slug.trim(),
    description: form.description.trim(),
    categoryId: form.categoryId,
    difficulty: form.difficulty,
    prepTimeMinutes: form.prepTimeMinutes,
    cookTimeMinutes: form.cookTimeMinutes,
    servings: form.servings,
  };
}

export function formToUpdateRecipeRequest(form: RecipeEditorValues): UpdateRecipeRequest {
  return {
    ...formToCreateRecipeRequest(form),
    calories: form.nutrition?.calories ?? null,
    proteinGrams: form.nutrition?.proteinGrams ?? null,
    carbohydratesGrams: form.nutrition?.carbohydratesGrams ?? null,
    fatGrams: form.nutrition?.fatGrams ?? null,
  };
}

export function formToIngredientRequest(
  ingredient: RecipeIngredientValues,
): RecipeIngredientRequest {
  return {
    name: ingredient.name.trim(),
    quantity: ingredient.quantity,
    unit: ingredient.unit.trim(),
    notes: nullableText(ingredient.notes),
  };
}

export function formToStepRequest(step: RecipeStepValues): RecipeStepRequest {
  return {
    description: step.description.trim(),
    timerMinutes: step.timerMinutes ?? null,
    imageUrl: nullableText(step.imageUrl),
  };
}
