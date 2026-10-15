import assert from "node:assert/strict";
import test from "node:test";
import {
  isRecipePublishReady,
  publishReadyRecipeSchema,
  recipeEditorSchema,
  recipeIngredientSchema,
  recipeStepSchema,
} from "../../lib/recipes/schema.ts";

const categoryId = "11111111-1111-4111-8111-111111111111";

function validDraftInput() {
  return {
    title: "  Pho bo  ",
    slug: "  pho-bo  ",
    description: "  Traditional noodle soup.  ",
    categoryId,
    difficulty: "Medium",
    prepTimeMinutes: "15",
    cookTimeMinutes: "90",
    servings: "4",
    ingredients: [],
    steps: [],
  };
}

test("recipe editor validates fields, trims text and coerces numeric inputs", () => {
  const result = recipeEditorSchema.parse(validDraftInput());

  assert.equal(result.title, "Pho bo");
  assert.equal(result.slug, "pho-bo");
  assert.equal(result.description, "Traditional noodle soup.");
  assert.equal(result.prepTimeMinutes, 15);
  assert.equal(result.cookTimeMinutes, 90);
  assert.equal(result.servings, 4);
  assert.deepEqual(result.ingredients, []);
  assert.deepEqual(result.steps, []);
});

test("recipe editor rejects invalid base fields", () => {
  const result = recipeEditorSchema.safeParse({
    ...validDraftInput(),
    title: "",
    categoryId: "not-a-guid",
    prepTimeMinutes: -1,
    servings: 0,
  });

  assert.equal(result.success, false);
  if (result.success) return;
  const paths = result.error.issues.map((issue) => issue.path.join("."));
  assert.ok(paths.includes("title"));
  assert.ok(paths.includes("categoryId"));
  assert.ok(paths.includes("prepTimeMinutes"));
  assert.ok(paths.includes("servings"));
});

test("ingredient validation requires a positive quantity and normalizes empty notes", () => {
  const ingredient = recipeIngredientSchema.parse({
    name: "  Rice noodles ",
    quantity: "250.5",
    unit: " g ",
    notes: "   ",
  });

  assert.deepEqual(ingredient, {
    name: "Rice noodles",
    quantity: 250.5,
    unit: "g",
    notes: undefined,
  });
  assert.equal(recipeIngredientSchema.safeParse({ name: "Salt", quantity: 0, unit: "g" }).success, false);
  assert.equal(recipeIngredientSchema.safeParse({ name: "", quantity: 1, unit: "" }).success, false);
});

test("step validation normalizes optional fields and rejects invalid values", () => {
  const step = recipeStepSchema.parse({
    description: "  Simmer gently. ",
    timerMinutes: "",
    imageUrl: "   ",
  });

  assert.deepEqual(step, {
    description: "Simmer gently.",
    timerMinutes: undefined,
    imageUrl: undefined,
  });
  assert.equal(recipeStepSchema.safeParse({ description: "Cook", timerMinutes: -1 }).success, false);
  assert.equal(recipeStepSchema.safeParse({ description: "Cook", imageUrl: "not-a-url" }).success, false);
});

test("optional nutrition numbers coerce values and normalize empty inputs to undefined", () => {
  const result = recipeEditorSchema.parse({
    ...validDraftInput(),
    nutrition: {
      calories: "450",
      proteinGrams: "22.5",
      carbohydratesGrams: "",
      fatGrams: null,
    },
  });

  assert.deepEqual(result.nutrition, {
    calories: 450,
    proteinGrams: 22.5,
    carbohydratesGrams: undefined,
    fatGrams: undefined,
  });
});

test("draft schema allows empty nested arrays while publish readiness requires both", () => {
  const draft = recipeEditorSchema.parse(validDraftInput());
  assert.equal(isRecipePublishReady(draft), false);

  const onlyIngredient = recipeEditorSchema.parse({
    ...validDraftInput(),
    ingredients: [{ name: "Beef", quantity: 500, unit: "g" }],
  });
  const incomplete = publishReadyRecipeSchema.safeParse(onlyIngredient);
  assert.equal(incomplete.success, false);
  if (!incomplete.success) {
    assert.deepEqual(incomplete.error.issues.map((issue) => issue.path), [["steps"]]);
  }

  const ready = recipeEditorSchema.parse({
    ...validDraftInput(),
    ingredients: [{ name: "Beef", quantity: 500, unit: "g" }],
    steps: [{ description: "Cook the beef." }],
  });
  assert.equal(isRecipePublishReady(ready), true);
});
