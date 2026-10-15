import assert from "node:assert/strict";
import test from "node:test";
import type { RecipeDetailDto } from "../../lib/recipes/contracts.ts";
import {
  formToCreateRecipeRequest,
  formToIngredientRequest,
  formToStepRequest,
  formToUpdateRecipeRequest,
  recipeDetailToForm,
} from "../../lib/recipes/form-mapper.ts";
import { recipeEditorSchema } from "../../lib/recipes/schema.ts";

const recipeId = "11111111-1111-4111-8111-111111111111";
const categoryId = "22222222-2222-4222-8222-222222222222";
const ingredientOneId = "33333333-3333-4333-8333-333333333333";
const ingredientTwoId = "44444444-4444-4444-8444-444444444444";
const stepOneId = "55555555-5555-4555-8555-555555555555";
const stepTwoId = "66666666-6666-4666-8666-666666666666";

function detail(nutrition: RecipeDetailDto["nutrition"] = null): RecipeDetailDto {
  return {
    id: recipeId,
    title: "Pho bo",
    slug: "pho-bo",
    description: "Traditional noodle soup.",
    categoryId,
    authorId: "author-1",
    prepTimeMinutes: 20,
    cookTimeMinutes: 120,
    servings: 6,
    difficulty: "Medium",
    status: "Draft",
    publishedAt: null,
    version: 12,
    nutrition,
    ingredients: [
      { id: ingredientTwoId, name: "Noodles", quantity: 500, unit: "g", notes: null, sortOrder: 2 },
      { id: ingredientOneId, name: "Beef", quantity: 300, unit: "g", notes: "Thinly sliced", sortOrder: 1 },
    ],
    steps: [
      { id: stepTwoId, stepNumber: 2, description: "Serve", timerMinutes: null, imageUrl: null },
      { id: stepOneId, stepNumber: 1, description: "Simmer", timerMinutes: 90, imageUrl: "https://images.example.test/step.jpg" },
    ],
    images: [],
    createdAt: "2026-10-15T00:00:00Z",
  };
}

test("API detail maps to ordered form arrays and normalizes null optionals", () => {
  const form = recipeDetailToForm(detail());

  assert.equal(form.ingredients[0].id, ingredientOneId);
  assert.equal(form.ingredients[0].quantity, 300);
  assert.equal(form.ingredients[1].notes, undefined);
  assert.equal(form.steps[0].id, stepOneId);
  assert.equal(form.steps[1].timerMinutes, undefined);
  assert.equal(form.steps[1].imageUrl, undefined);
  assert.equal(form.nutrition, undefined);
});

test("API nutrition maps without losing decimal nutrient values", () => {
  const form = recipeDetailToForm(detail({
    calories: 480,
    proteinGrams: 35.5,
    carbohydratesGrams: 62.25,
    fatGrams: 12.75,
  }));

  assert.deepEqual(form.nutrition, {
    calories: 480,
    proteinGrams: 35.5,
    carbohydratesGrams: 62.25,
    fatGrams: 12.75,
  });
});

test("form maps to the actual create and update payload field names", () => {
  const form = recipeEditorSchema.parse({
    ...recipeDetailToForm(detail()),
    nutrition: { calories: 480, proteinGrams: 35.5 },
  });

  assert.deepEqual(formToCreateRecipeRequest(form), {
    title: "Pho bo",
    slug: "pho-bo",
    description: "Traditional noodle soup.",
    categoryId,
    difficulty: "Medium",
    prepTimeMinutes: 20,
    cookTimeMinutes: 120,
    servings: 6,
  });
  assert.deepEqual(formToUpdateRecipeRequest(form), {
    title: "Pho bo",
    slug: "pho-bo",
    description: "Traditional noodle soup.",
    categoryId,
    difficulty: "Medium",
    prepTimeMinutes: 20,
    cookTimeMinutes: 120,
    servings: 6,
    calories: 480,
    proteinGrams: 35.5,
    carbohydratesGrams: null,
    fatGrams: null,
  });
});

test("nested request mapping uses quantity and description and converts empty optionals to null", () => {
  assert.deepEqual(formToIngredientRequest({
    name: "  Fish sauce ",
    quantity: 2,
    unit: " tbsp ",
    notes: "   ",
  }), {
    name: "Fish sauce",
    quantity: 2,
    unit: "tbsp",
    notes: null,
  });

  assert.deepEqual(formToStepRequest({
    description: "  Serve immediately. ",
    timerMinutes: undefined,
    imageUrl: " ",
  }), {
    description: "Serve immediately.",
    timerMinutes: null,
    imageUrl: null,
  });
});
