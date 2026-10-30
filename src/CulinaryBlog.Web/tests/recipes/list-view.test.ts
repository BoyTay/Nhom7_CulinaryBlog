import assert from "node:assert/strict";
import test from "node:test";
import type { RecipeDifficulty, RecipeListItemDto } from "../../lib/recipes/contracts.ts";
import { applyListView, parseListParams } from "../../lib/recipes/list-view.ts";

function recipe(index: number, over: Partial<RecipeListItemDto> = {}): RecipeListItemDto {
  return {
    id: `id-${index}`,
    title: `Món ${index}`,
    slug: `mon-${index}`,
    description: "",
    categoryId: "c1",
    authorId: "a1",
    prepTimeMinutes: 5,
    cookTimeMinutes: 30,
    servings: 2,
    difficulty: "Easy" as RecipeDifficulty,
    status: "Published",
    createdAt: new Date(Date.UTC(2026, 9, index)).toISOString(),
    categoryName: "Món chính",
    categorySlug: "mon-chinh",
    authorName: "A",
    thumbnailUrl: null,
    updatedAt: null,
    ...over,
  };
}

const defaults = { q: "", sort: "newest" as const, page: 1 };

test("parseListParams reads valid values and drops invalid ones", () => {
  assert.deepEqual(
    parseListParams({ q: " pho bo ", difficulty: "Hard", maxCook: "60", sort: "name", categoryId: "11111111-1111-4111-8111-111111111111", page: "3" }),
    { q: "pho bo", difficulty: "Hard", maxCook: 60, sort: "name", categoryId: "11111111-1111-4111-8111-111111111111", page: 3 },
  );
  assert.deepEqual(
    parseListParams({ difficulty: "Nope", maxCook: "45", sort: "abc", categoryId: "x", page: "0" }),
    { q: "", sort: "newest", page: 1 },
  );
  assert.deepEqual(parseListParams({ q: ["a", "b"], page: ["2"] }), { q: "", sort: "newest", page: 1 });
});

test("search ignores case and diacritics, so pho bo finds Phở bò", () => {
  const items = [recipe(1, { title: "Phở bò Hà Nội" }), recipe(2, { title: "Cá kho tộ" }), recipe(3, { title: "Bún chả", description: "ăn kèm phở bò" })];
  const result = applyListView(items, { ...defaults, q: "PHO BO" });
  assert.deepEqual(result.items.map((i) => i.id), ["id-3", "id-1"]);
  assert.equal(applyListView(items, { ...defaults, q: "đậu" }).totalCount, 0);
  assert.equal(applyListView([recipe(1, { title: "Đậu hũ" })], { ...defaults, q: "dau hu" }).totalCount, 1);
});

test("filters by difficulty, category and maximum cook time", () => {
  const items = [
    recipe(1, { difficulty: "Hard", cookTimeMinutes: 90 }),
    recipe(2, { difficulty: "Easy", cookTimeMinutes: 30 }),
    recipe(3, { difficulty: "Easy", cookTimeMinutes: 60, categoryId: "c2" }),
  ];
  assert.deepEqual(applyListView(items, { ...defaults, maxCook: 60 }).items.map((i) => i.id), ["id-3", "id-2"]);
  assert.deepEqual(applyListView(items, { ...defaults, difficulty: "Hard" }).items.map((i) => i.id), ["id-1"]);
  assert.deepEqual(applyListView(items, { ...defaults, categoryId: "c2" }).items.map((i) => i.id), ["id-3"]);
});

test("sorting supports newest, Vietnamese A-Z and shortest cook time", () => {
  const items = [
    recipe(1, { title: "Chè bưởi", cookTimeMinutes: 40 }),
    recipe(2, { title: "Ăn vặt", cookTimeMinutes: 10 }),
    recipe(3, { title: "Bánh xèo", cookTimeMinutes: 50 }),
  ];
  assert.deepEqual(applyListView(items, { ...defaults, sort: "newest" }).items.map((i) => i.id), ["id-3", "id-2", "id-1"]);
  assert.deepEqual(applyListView(items, { ...defaults, sort: "name" }).items.map((i) => i.title), ["Ăn vặt", "Bánh xèo", "Chè bưởi"]);
  assert.deepEqual(applyListView(items, { ...defaults, sort: "cook" }).items.map((i) => i.cookTimeMinutes), [10, 40, 50]);
});

test("paging uses 12 per page and clamps out-of-range pages", () => {
  const items = Array.from({ length: 30 }, (_, i) => recipe(i + 1));
  const first = applyListView(items, defaults);
  assert.equal(first.items.length, 12);
  assert.equal(first.totalPages, 3);
  assert.equal(first.totalCount, 30);
  const last = applyListView(items, { ...defaults, page: 99 });
  assert.equal(last.page, 3);
  assert.equal(last.items.length, 6);
});

test("an empty result still reports one page", () => {
  const result = applyListView([], defaults);
  assert.deepEqual({ totalCount: result.totalCount, totalPages: result.totalPages, page: result.page }, { totalCount: 0, totalPages: 1, page: 1 });
});
