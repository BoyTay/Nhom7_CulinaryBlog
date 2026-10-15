import { z } from "zod";

const emptyToUndefined = (value: unknown): unknown =>
  value === "" || value === null || value === undefined ? undefined : value;

const optionalTrimmedString = (maximumLength: number) =>
  z.preprocess(
    (value) => {
      if (typeof value !== "string") return value;
      const trimmed = value.trim();
      return trimmed === "" ? undefined : trimmed;
    },
    z.string().max(maximumLength).optional(),
  );

const requiredNonNegativeInteger = z.coerce.number().int().min(0);
const optionalNonNegativeNumber = z.preprocess(
  emptyToUndefined,
  z.coerce.number().min(0).optional(),
);
const optionalNonNegativeInteger = z.preprocess(
  emptyToUndefined,
  z.coerce.number().int().min(0).optional(),
);

export const recipeIngredientSchema = z.object({
  id: z.string().uuid().optional(),
  name: z.string().trim().min(1).max(200),
  quantity: z.coerce.number().positive(),
  unit: z.string().trim().min(1).max(50),
  notes: optionalTrimmedString(500),
});

export const recipeStepSchema = z.object({
  id: z.string().uuid().optional(),
  description: z.string().trim().min(1).max(2000),
  timerMinutes: optionalNonNegativeInteger,
  imageUrl: z.preprocess(
    (value) => {
      if (typeof value !== "string") return value;
      const trimmed = value.trim();
      return trimmed === "" ? undefined : trimmed;
    },
    z.string().url().max(2048).optional(),
  ),
});

export const recipeNutritionSchema = z.object({
  calories: optionalNonNegativeInteger,
  proteinGrams: optionalNonNegativeNumber,
  carbohydratesGrams: optionalNonNegativeNumber,
  fatGrams: optionalNonNegativeNumber,
});

export const recipeEditorSchema = z.object({
  title: z.string().trim().min(1).max(200),
  slug: z.string().trim().min(1).max(220),
  description: z.string().trim().min(1).max(5000),
  categoryId: z.string().uuid(),
  difficulty: z.enum(["Easy", "Medium", "Hard"]),
  prepTimeMinutes: requiredNonNegativeInteger,
  cookTimeMinutes: requiredNonNegativeInteger,
  servings: z.coerce.number().int().min(1),
  nutrition: recipeNutritionSchema.optional(),
  ingredients: z.array(recipeIngredientSchema).default([]),
  steps: z.array(recipeStepSchema).default([]),
});

/** Drafts may be incomplete; this schema is used only before a publish request. */
export const publishReadyRecipeSchema = recipeEditorSchema.superRefine((recipe, context) => {
  if (recipe.ingredients.length === 0) {
    context.addIssue({
      code: "custom",
      message: "A recipe needs at least one ingredient before publishing.",
      path: ["ingredients"],
    });
  }

  if (recipe.steps.length === 0) {
    context.addIssue({
      code: "custom",
      message: "A recipe needs at least one step before publishing.",
      path: ["steps"],
    });
  }
});

export type RecipeEditorInput = z.input<typeof recipeEditorSchema>;
export type RecipeEditorValues = z.output<typeof recipeEditorSchema>;
export type RecipeIngredientValues = z.output<typeof recipeIngredientSchema>;
export type RecipeStepValues = z.output<typeof recipeStepSchema>;

export function isRecipePublishReady(recipe: RecipeEditorValues): boolean {
  return publishReadyRecipeSchema.safeParse(recipe).success;
}
