import assert from "node:assert/strict";
import test from "node:test";
import { ApiError } from "../../lib/api/client.ts";
import type { RecipeClient } from "../../lib/recipes/client.ts";
import type { RecipeIngredientDto } from "../../lib/recipes/contracts.ts";
import {
  diffRecipeIngredients,
  mapIngredientProblemFieldErrors,
  RecipeIngredientSyncError,
  syncRecipeIngredients,
} from "../../lib/recipes/ingredients.ts";
import {
  publishReadyRecipeSchema,
  recipeEditorSchema,
  recipeIngredientSchema,
  type RecipeIngredientValues,
} from "../../lib/recipes/schema.ts";

const recipeId = "11111111-1111-4111-8111-111111111111";
const ingredientA = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const ingredientB = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
const ingredientC = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";

function ingredient(
  id: string | undefined,
  name: string,
  quantity = 1,
  unit = "kg",
  notes?: string,
): RecipeIngredientValues {
  return { id, name, quantity, unit, notes };
}

test("ingredient validation supports persisted IDs, decimal quantities, and optional notes", () => {
  assert.deepEqual(recipeIngredientSchema.parse({
    id: ingredientA,
    name: "  Xương bò  ",
    quantity: "1.5",
    unit: " kg ",
    notes: "  rửa sạch  ",
  }), {
    id: ingredientA,
    name: "Xương bò",
    quantity: 1.5,
    unit: "kg",
    notes: "rửa sạch",
  });
  assert.equal(recipeIngredientSchema.safeParse({
    name: "Muối",
    quantity: 0,
    unit: "g",
  }).success, false);
});

test("Draft may have zero ingredients while publish readiness still requires one", () => {
  const draft = recipeEditorSchema.parse({
    title: "Phở bò",
    slug: "pho-bo",
    description: "Món phở truyền thống.",
    categoryId: "22222222-2222-4222-8222-222222222222",
    difficulty: "Medium",
    prepTimeMinutes: 20,
    cookTimeMinutes: 120,
    servings: 4,
    ingredients: [],
    steps: [{ description: "Hầm nước dùng." }],
  });

  assert.equal(draft.ingredients.length, 0);
  const publishResult = publishReadyRecipeSchema.safeParse(draft);
  assert.equal(publishResult.success, false);
  if (!publishResult.success) {
    assert.ok(publishResult.error.issues.some((issue) => issue.path[0] === "ingredients"));
  }
});

test("diff identifies deleted, changed, and new rows without inventing reorder mutations", () => {
  const original = [
    ingredient(ingredientA, "Xương bò", 1.5),
    ingredient(ingredientB, "Bánh phở", 500, "g"),
  ];
  const current = [
    ingredient(ingredientA, "Xương bò", 2),
    ingredient(undefined, "Hành lá", 50, "g"),
  ];
  const diff = diffRecipeIngredients(original, current);

  assert.deepEqual(diff.map((mutation) => mutation.kind), ["delete", "update", "create"]);
  assert.equal(diff[0].kind === "delete" && diff[0].ingredientId, ingredientB);
  assert.equal(diff[1].kind === "update" && diff[1].ingredientId, ingredientA);
  assert.equal(diff[2].kind === "create" && diff[2].index, 1);
  assert.deepEqual(diffRecipeIngredients(original, [...original].reverse()), []);
});

test("ingredient mutations run sequentially and always consume the latest ETag", async () => {
  const calls: string[] = [];
  const client = ingredientClient({
    async deleteIngredient(_recipeId, id, eTag) {
      calls.push(`delete:${id}:${eTag}`);
      return { data: undefined, eTag: '"11"' };
    },
    async updateIngredient(_recipeId, id, _request, eTag) {
      calls.push(`update:${id}:${eTag}`);
      return { data: ingredientDto(id), eTag: '"12"' };
    },
    async createIngredient(_recipeId, _request, eTag) {
      calls.push(`create:${eTag}`);
      return { data: { id: ingredientC }, eTag: '"13"' };
    },
  });
  const original = [
    ingredient(ingredientA, "Xương bò"),
    ingredient(ingredientB, "Bánh phở", 500, "g"),
  ];
  const current = [
    ingredient(ingredientA, "Xương bò", 2),
    ingredient(undefined, "Hành lá", 50, "g"),
  ];

  const result = await syncRecipeIngredients(client, recipeId, '"10"', original, current);

  assert.deepEqual(calls, [
    `delete:${ingredientB}:"10"`,
    `update:${ingredientA}:"11"`,
    "create:\"12\"",
  ]);
  assert.equal(result.eTag, '"13"');
  assert.equal(result.ingredients[1].id, ingredientC);
  assert.deepEqual(result.persistedIngredients, result.ingredients);
});

test("sync stops at the first failure and reports completed state for a safe retry", async () => {
  const calls: string[] = [];
  const client = ingredientClient({
    async deleteIngredient(_recipeId, id, eTag) {
      calls.push(`delete:${id}:${eTag}`);
      return { data: undefined, eTag: '"21"' };
    },
    async updateIngredient(_recipeId, id, _request, eTag) {
      calls.push(`update:${id}:${eTag}`);
      throw new ApiError(503, { type: "SERVICE_UNAVAILABLE" });
    },
    async createIngredient() {
      calls.push("create");
      return { data: { id: ingredientC }, eTag: '"22"' };
    },
  });
  const original = [
    ingredient(ingredientA, "Xương bò"),
    ingredient(ingredientB, "Bánh phở", 500, "g"),
  ];
  const current = [
    ingredient(ingredientA, "Xương bò", 2),
    ingredient(undefined, "Hành lá", 50, "g"),
  ];

  await assert.rejects(
    syncRecipeIngredients(client, recipeId, '"20"', original, current),
    (error: unknown) => {
      assert.ok(error instanceof RecipeIngredientSyncError);
      assert.equal(error.eTag, '"21"');
      assert.equal(error.failedMutation.kind, "update");
      assert.deepEqual(error.persistedIngredients.map((item) => item.id), [ingredientA]);
      assert.deepEqual(error.ingredients, current);
      return true;
    },
  );
  assert.deepEqual(calls, [
    `delete:${ingredientB}:"20"`,
    `update:${ingredientA}:"21"`,
  ]);
});

test("created IDs are preserved when a later create fails", async () => {
  let createCount = 0;
  const client = ingredientClient({
    async createIngredient() {
      createCount += 1;
      if (createCount === 1) return { data: { id: ingredientC }, eTag: '"31"' };
      throw new ApiError(422, { type: "VALIDATION_ERROR" });
    },
  });
  const current = [
    ingredient(undefined, "Hành lá", 50, "g"),
    ingredient(undefined, "Gừng", 20, "g"),
  ];

  await assert.rejects(
    syncRecipeIngredients(client, recipeId, '"30"', [], current),
    (error: unknown) => {
      assert.ok(error instanceof RecipeIngredientSyncError);
      assert.equal(error.eTag, '"31"');
      assert.equal(error.ingredients[0].id, ingredientC);
      assert.equal(error.ingredients[1].id, undefined);
      assert.deepEqual(error.persistedIngredients.map((item) => item.id), [ingredientC]);
      return true;
    },
  );
});

test("RFC 7807 ingredient errors map to row field names", () => {
  assert.deepEqual(mapIngredientProblemFieldErrors({
    type: "VALIDATION_ERROR",
    errors: {
      Name: ["Name is required."],
      Quantity: ["Quantity must be positive."],
      Unknown: ["Ignored."],
    },
  }), {
    name: "Name is required.",
    quantity: "Quantity must be positive.",
  });
});

type IngredientClient = Pick<
  RecipeClient,
  "createIngredient" | "updateIngredient" | "deleteIngredient"
>;

function ingredientClient(overrides: Partial<IngredientClient>): IngredientClient {
  return {
    async createIngredient() {
      throw new Error("Unexpected createIngredient call");
    },
    async updateIngredient() {
      throw new Error("Unexpected updateIngredient call");
    },
    async deleteIngredient() {
      throw new Error("Unexpected deleteIngredient call");
    },
    ...overrides,
  };
}

function ingredientDto(id: string): RecipeIngredientDto {
  return {
    id,
    name: "Xương bò",
    quantity: 2,
    unit: "kg",
    notes: null,
    sortOrder: 1,
  };
}

