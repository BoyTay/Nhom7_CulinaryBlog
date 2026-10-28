import assert from "node:assert/strict";
import test from "node:test";
import { ApiError } from "../../lib/api/client.ts";
import type { RecipeClient } from "../../lib/recipes/client.ts";
import type {
  CreateRecipeRequest,
  RecipeDetailDto,
  UpdateRecipeRequest,
} from "../../lib/recipes/contracts.ts";
import {
  canAccessRecipeEditor,
  createRecipeEditorDefaults,
  mapRecipeProblemFieldErrors,
  MissingRecipeEtagError,
  moveRecipeEditorStep,
  recipeEditorConflictKind,
  RecipeCreatedContinuationError,
  replaceRecipeEtag,
  saveRecipeBase,
  slugifyRecipeTitle,
} from "../../lib/recipes/editor.ts";
import { recipeDetailToForm } from "../../lib/recipes/form-mapper.ts";
import { recipeEditorSchema, type RecipeEditorValues } from "../../lib/recipes/schema.ts";

const recipeId = "11111111-1111-4111-8111-111111111111";
const categoryId = "22222222-2222-4222-8222-222222222222";

function detail(overrides: Partial<RecipeDetailDto> = {}): RecipeDetailDto {
  return {
    id: recipeId,
    title: "Phở bò Hà Nội",
    slug: "pho-bo-ha-noi",
    description: "Nước dùng trong và thơm.",
    categoryId,
    authorId: "author-1",
    prepTimeMinutes: 30,
    cookTimeMinutes: 120,
    servings: 4,
    difficulty: "Medium",
    status: "Draft",
    publishedAt: null,
    version: 7,
    nutrition: {
      calories: 450,
      proteinGrams: 30.5,
      carbohydratesGrams: 52,
      fatGrams: 12.25,
    },
    ingredients: [],
    steps: [],
    images: [],
    createdAt: "2026-10-15T00:00:00Z",
    categoryName: "Món nước",
    categorySlug: "mon-nuoc",
    authorName: "Phan Hiếu",
    ...overrides,
  };
}

function validValues(): RecipeEditorValues {
  return recipeEditorSchema.parse(recipeDetailToForm(detail()));
}

test("create editor starts as an incomplete Draft form without unfinished nested data", () => {
  const defaults = createRecipeEditorDefaults();

  assert.deepEqual(defaults, {
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
  });
  assert.equal(recipeEditorSchema.safeParse(defaults).success, false);
});

test("edit detail populates the editor base and nutrition values", () => {
  const form = recipeDetailToForm(detail());

  assert.equal(form.title, "Phở bò Hà Nội");
  assert.equal(form.slug, "pho-bo-ha-noi");
  assert.equal(form.categoryId, categoryId);
  assert.equal(form.prepTimeMinutes, 30);
  assert.deepEqual(form.nutrition, {
    calories: 450,
    proteinGrams: 30.5,
    carbohydratesGrams: 52,
    fatGrams: 12.25,
  });
});

test("Zod integration coerces browser numeric strings before an editor save", () => {
  const parsed = recipeEditorSchema.parse({
    ...createRecipeEditorDefaults(),
    title: "Phở bò",
    slug: "pho-bo",
    description: "Món phở truyền thống.",
    categoryId,
    prepTimeMinutes: "20",
    cookTimeMinutes: "120",
    servings: "6",
    nutrition: { calories: "480", proteinGrams: "35.5" },
  });

  assert.equal(parsed.prepTimeMinutes, 20);
  assert.equal(parsed.servings, 6);
  assert.equal(parsed.nutrition?.calories, 480);
  assert.equal(parsed.nutrition?.proteinGrams, 35.5);
});

test("wizard navigation changes only the current step and leaves form values intact", () => {
  const values = validValues();
  const sameValues = values;

  assert.equal(moveRecipeEditorStep(1, "next"), 2);
  assert.equal(moveRecipeEditorStep(2, "next"), 3);
  assert.equal(moveRecipeEditorStep(3, "previous"), 2);
  assert.equal(moveRecipeEditorStep(4, "next"), 4);
  assert.strictEqual(values, sameValues);
  assert.equal(values.title, "Phở bò Hà Nội");
});

test("create saves the actual Draft contract, then uses its ETag to persist nutrition", async () => {
  let captured: CreateRecipeRequest | undefined;
  let continuation: { request: UpdateRecipeRequest; eTag: string } | undefined;
  const client = {
    async create(request: CreateRecipeRequest) {
      captured = request;
      return { data: { id: recipeId }, eTag: null };
    },
    async detail() {
      return { data: detail(), eTag: '"1"' };
    },
    async update(_id: string, request: UpdateRecipeRequest, eTag: string) {
      continuation = { request, eTag };
      return { data: detail(), eTag: '"2"' };
    },
  } satisfies Pick<RecipeClient, "create" | "detail" | "update">;

  const result = await saveRecipeBase(client, { mode: "create", values: validValues() });

  assert.equal(result.kind, "created");
  if (result.kind === "created") {
    assert.equal(result.id, recipeId);
    assert.equal(result.eTag, '"2"');
    assert.equal(result.detail.id, recipeId);
  }
  assert.deepEqual(captured, {
    title: "Phở bò Hà Nội",
    slug: "pho-bo-ha-noi",
    description: "Nước dùng trong và thơm.",
    categoryId,
    difficulty: "Medium",
    prepTimeMinutes: 30,
    cookTimeMinutes: 120,
    servings: 4,
  });
  assert.equal("status" in (captured ?? {}), false);
  assert.equal("ingredients" in (captured ?? {}), false);
  assert.equal("steps" in (captured ?? {}), false);
  assert.equal(continuation?.eTag, '"1"');
  assert.equal(continuation?.request.calories, 450);
  assert.equal(continuation?.request.proteinGrams, 30.5);
});

test("edit maps nutrition, sends If-Match, and replaces the local ETag", async () => {
  let capturedRequest: UpdateRecipeRequest | undefined;
  let capturedEtag = "";
  const updated = detail({ version: 8 });
  const client = {
    async create() {
      throw new Error("create must not run for edit");
    },
    async detail() {
      throw new Error("detail must not run for edit");
    },
    async update(_id: string, request: UpdateRecipeRequest, eTag: string) {
      capturedRequest = request;
      capturedEtag = eTag;
      return { data: updated, eTag: '"8"' };
    },
  } satisfies Pick<RecipeClient, "create" | "detail" | "update">;

  const result = await saveRecipeBase(client, {
    mode: "edit",
    id: recipeId,
    eTag: '"7"',
    values: validValues(),
  });

  assert.equal(capturedEtag, '"7"');
  assert.equal(capturedRequest?.calories, 450);
  assert.equal(capturedRequest?.proteinGrams, 30.5);
  assert.equal(result.kind, "updated");
  if (result.kind === "updated") assert.equal(result.eTag, '"8"');
  assert.equal(replaceRecipeEtag('"8"', null), null);
});

test("edit refuses to submit without an authoritative ETag", async () => {
  const client = {
    async create() {
      throw new Error("not used");
    },
    async detail() {
      throw new Error("not used");
    },
    async update() {
      throw new Error("update must not run without an ETag");
    },
  } satisfies Pick<RecipeClient, "create" | "detail" | "update">;

  await assert.rejects(
    saveRecipeBase(client, { mode: "edit", id: recipeId, eTag: null, values: validValues() }),
    MissingRecipeEtagError,
  );
});

test("a post-create continuation failure exposes the created id and prevents duplicate creation", async () => {
  const client = {
    async create() {
      return { data: { id: recipeId }, eTag: null };
    },
    async detail() {
      throw new ApiError(503, { type: "SERVICE_UNAVAILABLE" });
    },
    async update() {
      throw new Error("not reached");
    },
  } satisfies Pick<RecipeClient, "create" | "detail" | "update">;

  await assert.rejects(
    saveRecipeBase(client, { mode: "create", values: validValues() }),
    (error: unknown) => {
      assert.ok(error instanceof RecipeCreatedContinuationError);
      assert.equal(error.recipeId, recipeId);
      return true;
    },
  );
});

test("RFC 7807 field errors map to Recipe Hook Form paths", () => {
  assert.deepEqual(mapRecipeProblemFieldErrors({
    type: "VALIDATION_ERROR",
    errors: {
      Title: ["Title is required."],
      PrepTimeMinutes: ["Preparation time is invalid."],
      Calories: ["Calories must be positive."],
      UnknownBackendField: ["Ignored."],
    },
  }), {
    title: "Title is required.",
    prepTimeMinutes: "Preparation time is invalid.",
    "nutrition.calories": "Calories must be positive.",
  });
});

test("concurrency and ETag precondition problems require an explicit reload", () => {
  assert.equal(recipeEditorConflictKind(new ApiError(422, {
    type: "RECIPE_CONCURRENCY_CONFLICT",
  })), "stale");
  assert.equal(recipeEditorConflictKind(new ApiError(428, {
    type: "PRECONDITION_REQUIRED",
  })), "precondition-required");
  assert.equal(recipeEditorConflictKind(new ApiError(400, {
    type: "INVALID_ETAG",
  })), "invalid-etag");
  assert.equal(recipeEditorConflictKind(new ApiError(422, {
    type: "VALIDATION_ERROR",
  })), null);
});

test("Author and Admin roles pass the client route gate", () => {
  assert.equal(canAccessRecipeEditor(["Author"]), true);
  assert.equal(canAccessRecipeEditor(["admin"]), true);
  assert.equal(canAccessRecipeEditor(["User"]), false);
  assert.equal(canAccessRecipeEditor([]), false);
});

test("Vietnamese titles generate editable Recipe-local slugs", () => {
  assert.equal(slugifyRecipeTitle("  Phở bò Hà Nội!  "), "pho-bo-ha-noi");
  assert.equal(slugifyRecipeTitle("Cá kho tộ"), "ca-kho-to");
});

test("a just-created recipe reopens the editor on the photo step instead of step 1", async () => {
  const { initialEditorStep } = await import("../../lib/recipes/editor.ts");
  assert.equal(initialEditorStep("1"), 4);
  assert.equal(initialEditorStep("partial"), 4);
  assert.equal(initialEditorStep(undefined), 1);
  assert.equal(initialEditorStep("something-else"), 1);
});
