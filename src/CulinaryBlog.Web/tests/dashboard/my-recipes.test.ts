import assert from "node:assert/strict";
import test from "node:test";
import {
  actionConfirmation,
  availableActions,
  countMissingIngredients,
  draftNote,
  missingParts,
  filterByStatus,
  parseStatusTab,
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
  assert.deepEqual(availableActions("Draft"), ["publish", "archive", "delete"]);
  assert.deepEqual(availableActions("Published"), ["unpublish", "archive", "delete"]);
  assert.deepEqual(availableActions("Archived"), ["restore", "delete"]);
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

test("filterByStatus narrows the list by tab and keeps order", () => {
  const items = [
    { id: "1", status: "Draft" },
    { id: "2", status: "Published" },
    { id: "3", status: "Archived" },
    { id: "4", status: "Published" },
  ] as Array<Pick<RecipeListItemDto, "id" | "status">>;

  assert.deepEqual(filterByStatus(items, "all").map((i) => i.id), ["1", "2", "3", "4"]);
  assert.deepEqual(filterByStatus(items, "draft").map((i) => i.id), ["1"]);
  assert.deepEqual(filterByStatus(items, "published").map((i) => i.id), ["2", "4"]);
  assert.deepEqual(filterByStatus(items, "archived").map((i) => i.id), ["3"]);
});

test("parseStatusTab accepts the four tabs and falls back to all", () => {
  assert.equal(parseStatusTab("draft"), "draft");
  assert.equal(parseStatusTab("published"), "published");
  assert.equal(parseStatusTab("archived"), "archived");
  assert.equal(parseStatusTab("xyz"), "all");
  assert.equal(parseStatusTab(undefined), "all");
});

test("restoring an archived recipe sends it back to draft through unpublish", async () => {
  const calls: string[] = [];
  const client = {
    detail: async () => ({ data: {} as never, eTag: '"1"' }),
    publish: async () => { throw new Error("unexpected"); },
    unpublish: async (id: string, eTag: string) => { calls.push(`unpublish:${id}:${eTag}`); return { data: {} as never, eTag: null }; },
    archive: async () => { throw new Error("unexpected"); },
    delete: async () => { throw new Error("unexpected"); },
  };
  await runRecipeAction(client, "r9", "restore");
  assert.deepEqual(calls, ['unpublish:r9:"1"']);
});

test("a failed publish names the recipe when the title is known", () => {
  assert.equal(
    explainLifecycleError("RECIPE_PUBLISH_INCOMPLETE", "Cá kho tộ"),
    "Chưa thể xuất bản “Cá kho tộ”. Công thức cần ít nhất 1 nguyên liệu và 1 bước thực hiện.",
  );
});

test("unpublishing, archiving and deleting ask first; publishing and restoring do not", () => {
  assert.equal(actionConfirmation("delete", "Phở")?.title, "Xóa “Phở”?");
  assert.equal(actionConfirmation("delete", "Phở")?.destructive, true);
  assert.equal(actionConfirmation("archive", "Phở")?.confirmLabel, "Lưu trữ");
  assert.equal(actionConfirmation("unpublish", "Phở")?.title, "Hủy xuất bản “Phở”?");
  assert.equal(actionConfirmation("publish", "Phở"), null);
  assert.equal(actionConfirmation("restore", "Phở"), null);
});

test("missingParts names what a draft lacks", () => {
  assert.equal(missingParts({ ingredients: [], steps: [] }), "nguyên liệu và các bước");
  assert.equal(missingParts({ ingredients: [], steps: [1] }), "nguyên liệu");
  assert.equal(missingParts({ ingredients: [1], steps: [] }), "các bước");
  assert.equal(missingParts({ ingredients: [1], steps: [1] }), null);
});

test("countMissingIngredients ignores drafts that only lack steps", () => {
  const gaps = new Map([["a", "nguyên liệu và các bước"], ["b", "nguyên liệu"], ["c", "các bước"], ["d", null]]);
  assert.equal(countMissingIngredients(gaps), 2);
});

test("draftNote covers loading, missing parts, a missing photo and a finished draft", () => {
  assert.equal(draftNote(undefined, false), "Bản nháp chưa xuất bản");
  assert.equal(draftNote("nguyên liệu và các bước", true), "Còn thiếu nguyên liệu và các bước");
  assert.equal(draftNote(null, false), "Chưa có ảnh đại diện");
  assert.equal(draftNote(null, true), null);
});
