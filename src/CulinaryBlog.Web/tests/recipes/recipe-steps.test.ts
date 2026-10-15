import assert from "node:assert/strict";
import test from "node:test";
import { ApiError } from "../../lib/api/client.ts";
import type { RecipeClient } from "../../lib/recipes/client.ts";
import type { RecipeStepDto, RecipeStepRequest } from "../../lib/recipes/contracts.ts";
import {
  publishReadyRecipeSchema,
  recipeEditorSchema,
  recipeStepSchema,
  type RecipeStepValues,
} from "../../lib/recipes/schema.ts";
import {
  diffRecipeSteps,
  mapStepProblemFieldErrors,
  RecipeStepSyncError,
  syncRecipeSteps,
} from "../../lib/recipes/steps.ts";

const recipeId = "11111111-1111-4111-8111-111111111111";
const stepA = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const stepB = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
const stepC = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";

function step(
  id: string | undefined,
  description: string,
  timerMinutes?: number,
  imageUrl?: string,
): RecipeStepValues {
  return { id, description, timerMinutes, imageUrl };
}

test("step validation supports IDs and normalizes optional timer and image values", () => {
  assert.deepEqual(recipeStepSchema.parse({
    id: stepA,
    description: "  Hầm xương lấy nước dùng.  ",
    timerMinutes: "120",
    imageUrl: " https://cdn.example.test/step.jpg ",
  }), {
    id: stepA,
    description: "Hầm xương lấy nước dùng.",
    timerMinutes: 120,
    imageUrl: "https://cdn.example.test/step.jpg",
  });
  assert.deepEqual(recipeStepSchema.parse({
    description: "Nêm gia vị.",
    timerMinutes: "",
    imageUrl: "",
  }), {
    description: "Nêm gia vị.",
    timerMinutes: undefined,
    imageUrl: undefined,
  });
  assert.equal(recipeStepSchema.safeParse({ description: "", timerMinutes: -1 }).success, false);
});

test("Draft may have zero steps while publish readiness still requires one", () => {
  const draft = recipeEditorSchema.parse({
    title: "Phở bò",
    slug: "pho-bo",
    description: "Món phở truyền thống.",
    categoryId: "22222222-2222-4222-8222-222222222222",
    difficulty: "Medium",
    prepTimeMinutes: 20,
    cookTimeMinutes: 120,
    servings: 4,
    ingredients: [{ name: "Xương bò", quantity: 1, unit: "kg" }],
    steps: [],
  });

  assert.equal(draft.steps.length, 0);
  const publishResult = publishReadyRecipeSchema.safeParse(draft);
  assert.equal(publishResult.success, false);
  if (!publishResult.success) {
    assert.ok(publishResult.error.issues.some((issue) => issue.path[0] === "steps"));
  }
});

test("diff identifies deleted, changed, and new steps without sending reorder operations", () => {
  const original = [
    step(stepA, "Hầm xương.", 120),
    step(stepB, "Nêm gia vị."),
  ];
  const current = [
    step(stepA, "Hầm xương thật kỹ.", 120),
    step(undefined, "Hoàn thiện món ăn.", 5),
  ];
  const diff = diffRecipeSteps(original, current);

  assert.deepEqual(diff.map((mutation) => mutation.kind), ["delete", "update", "create"]);
  assert.equal(diff[0].kind === "delete" && diff[0].stepId, stepB);
  assert.equal(diff[1].kind === "update" && diff[1].stepId, stepA);
  assert.equal(diff[2].kind === "create" && diff[2].index, 1);
  assert.deepEqual(diffRecipeSteps(original, [...original].reverse()), []);
});

test("step mutations are sequential, append in form order, and consume the latest ETag", async () => {
  const calls: string[] = [];
  const createdRequests: RecipeStepRequest[] = [];
  const client = stepClient({
    async deleteStep(_recipeId, id, eTag) {
      calls.push(`delete:${id}:${eTag}`);
      return { data: undefined, eTag: '"41"' };
    },
    async updateStep(_recipeId, id, _request, eTag) {
      calls.push(`update:${id}:${eTag}`);
      return { data: stepDto(id), eTag: '"42"' };
    },
    async createStep(_recipeId, request, eTag) {
      calls.push(`create:${eTag}`);
      createdRequests.push(request);
      return { data: { id: stepC }, eTag: '"43"' };
    },
  });
  const original = [step(stepA, "Hầm xương."), step(stepB, "Nêm gia vị.")];
  const current = [
    step(stepA, "Hầm xương kỹ hơn.", 120),
    step(undefined, "Hoàn thiện.", 5, "https://cdn.example.test/final.jpg"),
  ];

  const result = await syncRecipeSteps(client, recipeId, '"40"', original, current);

  assert.deepEqual(calls, [
    `delete:${stepB}:"40"`,
    `update:${stepA}:"41"`,
    "create:\"42\"",
  ]);
  assert.deepEqual(createdRequests, [{
    description: "Hoàn thiện.",
    timerMinutes: 5,
    imageUrl: "https://cdn.example.test/final.jpg",
  }]);
  assert.equal("stepNumber" in createdRequests[0], false);
  assert.equal(result.eTag, '"43"');
  assert.equal(result.steps[1].id, stepC);
  assert.deepEqual(result.persistedSteps, result.steps);
});

test("step sync stops at the first failure and exposes completed state for retry", async () => {
  const calls: string[] = [];
  const client = stepClient({
    async deleteStep(_recipeId, id, eTag) {
      calls.push(`delete:${id}:${eTag}`);
      return { data: undefined, eTag: '"51"' };
    },
    async updateStep(_recipeId, id, _request, eTag) {
      calls.push(`update:${id}:${eTag}`);
      throw new ApiError(503, { type: "SERVICE_UNAVAILABLE" });
    },
    async createStep() {
      calls.push("create");
      return { data: { id: stepC }, eTag: '"52"' };
    },
  });
  const original = [step(stepA, "Hầm xương."), step(stepB, "Nêm gia vị.")];
  const current = [step(stepA, "Hầm kỹ hơn."), step(undefined, "Hoàn thiện.")];

  await assert.rejects(
    syncRecipeSteps(client, recipeId, '"50"', original, current),
    (error: unknown) => {
      assert.ok(error instanceof RecipeStepSyncError);
      assert.equal(error.eTag, '"51"');
      assert.equal(error.failedMutation.kind, "update");
      assert.deepEqual(error.persistedSteps.map((item) => item.id), [stepA]);
      assert.deepEqual(error.steps, current);
      return true;
    },
  );
  assert.deepEqual(calls, [`delete:${stepB}:"50"`, `update:${stepA}:"51"`]);
});

test("created step IDs survive a later create failure", async () => {
  let createCount = 0;
  const client = stepClient({
    async createStep() {
      createCount += 1;
      if (createCount === 1) return { data: { id: stepC }, eTag: '"61"' };
      throw new ApiError(422, { type: "VALIDATION_ERROR" });
    },
  });
  const current = [step(undefined, "Bước mới một."), step(undefined, "Bước mới hai.")];

  await assert.rejects(
    syncRecipeSteps(client, recipeId, '"60"', [], current),
    (error: unknown) => {
      assert.ok(error instanceof RecipeStepSyncError);
      assert.equal(error.eTag, '"61"');
      assert.equal(error.steps[0].id, stepC);
      assert.equal(error.steps[1].id, undefined);
      assert.deepEqual(error.persistedSteps.map((item) => item.id), [stepC]);
      return true;
    },
  );
});

test("RFC 7807 step errors map to step row fields", () => {
  assert.deepEqual(mapStepProblemFieldErrors({
    type: "VALIDATION_ERROR",
    errors: {
      Description: ["Description is required."],
      TimerMinutes: ["Timer must be non-negative."],
      ImageUrl: ["Image URL is invalid."],
      Unknown: ["Ignored."],
    },
  }), {
    description: "Description is required.",
    timerMinutes: "Timer must be non-negative.",
    imageUrl: "Image URL is invalid.",
  });
});

type StepClient = Pick<RecipeClient, "createStep" | "updateStep" | "deleteStep">;

function stepClient(overrides: Partial<StepClient>): StepClient {
  return {
    async createStep() {
      throw new Error("Unexpected createStep call");
    },
    async updateStep() {
      throw new Error("Unexpected updateStep call");
    },
    async deleteStep() {
      throw new Error("Unexpected deleteStep call");
    },
    ...overrides,
  };
}

function stepDto(id: string): RecipeStepDto {
  return {
    id,
    stepNumber: 1,
    description: "Hầm xương kỹ hơn.",
    timerMinutes: 120,
    imageUrl: null,
  };
}
