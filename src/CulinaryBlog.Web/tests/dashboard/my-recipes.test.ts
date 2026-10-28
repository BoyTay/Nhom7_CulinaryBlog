import assert from "node:assert/strict";
import test from "node:test";
import {
  availableActions,
  explainLifecycleError,
  runRecipeAction,
  summarizeMyRecipes,
} from "../../lib/dashboard/my-recipes.ts";
import type { RecipeListItemDto, RecipeStatus } from "../../lib/recipes/contracts.ts";

function item(status: RecipeStatus): Pick<RecipeListItemDto, "status"> {
  return { status };
}

test("summarizeMyRecipes counts each status and tolerates an empty list", () => {
  assert.deepEqual(summarizeMyRecipes([]), { draft: 0, published: 0, archived: 0 });
  assert.deepEqual(
    summarizeMyRecipes([item("Draft"), item("Draft"), item("Published"), item("Archived")]),
    { draft: 2, published: 1, archived: 1 },
  );
});

test("availableActions follows the recipe lifecycle", () => {
  assert.deepEqual(availableActions("Draft"), ["publish", "delete"]);
  assert.deepEqual(availableActions("Published"), ["unpublish", "archive"]);
  assert.deepEqual(availableActions("Archived"), ["delete"]);
});

test("explainLifecycleError explains incomplete publishes and falls back for unknown codes", () => {
  assert.match(explainLifecycleError("RECIPE_PUBLISH_INCOMPLETE"), /nguyên liệu.*bước/);
  assert.match(explainLifecycleError("RECIPE_CONCURRENCY_CONFLICT"), /tải lại/);
  assert.equal(explainLifecycleError("SOMETHING_NEW"), "Không thể thực hiện thao tác. Vui lòng thử lại.");
  assert.equal(explainLifecycleError(undefined), "Không thể thực hiện thao tác. Vui lòng thử lại.");
});

test("runRecipeAction reads the current ETag first and passes it to the mutation", async () => {
  const calls: string[] = [];
  const client = {
    detail: async (id: string) => { calls.push(`detail:${id}`); return { data: {} as never, eTag: '"9"' }; },
    publish: async (id: string, eTag: string) => { calls.push(`publish:${id}:${eTag}`); return { data: {} as never, eTag: null }; },
    unpublish: async () => { throw new Error("unexpected"); },
    archive: async () => { throw new Error("unexpected"); },
    delete: async () => { throw new Error("unexpected"); },
  };

  await runRecipeAction(client, "r1", "publish");

  assert.deepEqual(calls, ["detail:r1", 'publish:r1:"9"']);
});

test("runRecipeAction refuses to mutate when the server returns no ETag", async () => {
  const client = {
    detail: async () => ({ data: {} as never, eTag: null }),
    publish: async () => { throw new Error("must not be called"); },
    unpublish: async () => { throw new Error("must not be called"); },
    archive: async () => { throw new Error("must not be called"); },
    delete: async () => { throw new Error("must not be called"); },
  };

  await assert.rejects(() => runRecipeAction(client, "r1", "delete"), /ETag/);
});
