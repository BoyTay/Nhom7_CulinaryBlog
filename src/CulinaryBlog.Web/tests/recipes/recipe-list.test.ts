import assert from "node:assert/strict";
import test from "node:test";
import type { RecipeListItemDto } from "../../lib/recipes/contracts.ts";
import {
  createRecipeListViewModel,
  paginateLocally,
  parseRecipeListFilters,
  toPublicRecipeFilters,
} from "../../lib/recipes/list.ts";

function recipe(index: number): RecipeListItemDto {
  return {
    id: `00000000-0000-4000-8000-${String(index).padStart(12, "0")}`,
    title: `Recipe ${index}`,
    slug: `recipe-${index}`,
    description: `Description ${index}`,
    categoryId: "11111111-1111-4111-8111-111111111111",
    authorId: "author-1",
    prepTimeMinutes: 10,
    cookTimeMinutes: 20 + index,
    servings: 4,
    difficulty: index % 2 === 0 ? "Easy" : "Medium",
    status: "Published",
    createdAt: "2026-10-15T00:00:00Z",
  };
}

test("populated results map only real list fields and use the design placeholder", () => {
  const model = createRecipeListViewModel([recipe(1), recipe(2)]);

  assert.equal(model.state, "populated");
  assert.equal(model.totalCount, 2);
  if (model.state !== "populated") return;
  assert.deepEqual(model.cards[0], {
    id: recipe(1).id,
    href: `/recipes/${recipe(1).id}`,
    title: "Recipe 1",
    cookTimeLabel: "21 phút",
    difficultyLabel: "Trung bình",
    usesPlaceholder: true,
  });
  assert.ok(model.cards.every((card) => card.usesPlaceholder));
  assert.equal("authorName" in model.cards[0], false);
  assert.equal("categoryName" in model.cards[0], false);
  assert.equal("imageUrl" in model.cards[0], false);
});

test("empty and API error states are distinct", () => {
  assert.deepEqual(createRecipeListViewModel([]), {
    state: "empty",
    cards: [],
    totalCount: 0,
  });
  assert.deepEqual(createRecipeListViewModel([], true), {
    state: "error",
    cards: [],
    totalCount: 0,
  });
});

test("filter parsing accepts current backend filters and enforces Published status", () => {
  const parsed = parseRecipeListFilters({
    categoryId: "11111111-1111-4111-8111-111111111111",
    difficulty: "Hard",
    status: "Published",
    page: "3",
  });

  assert.equal(parsed.success, true);
  if (!parsed.success) return;
  assert.deepEqual(parsed.value, {
    categoryId: "11111111-1111-4111-8111-111111111111",
    difficulty: "Hard",
    page: 3,
  });
  assert.deepEqual(toPublicRecipeFilters(parsed.value), {
    categoryId: "11111111-1111-4111-8111-111111111111",
    difficulty: "Hard",
    status: "Published",
  });
});

test("invalid filter values are rejected before calling the API", () => {
  for (const params of [
    { categoryId: "not-a-guid" },
    { difficulty: "Impossible" },
    { page: "0" },
    { page: "1.5" },
    { status: "Draft" },
    { difficulty: ["Easy", "Hard"] },
  ]) {
    assert.equal(parseRecipeListFilters(params).success, false);
  }
});

test("local pagination slices results and clamps pages to the available range", () => {
  const recipes = Array.from({ length: 25 }, (_, index) => recipe(index + 1));
  const secondPage = paginateLocally(recipes, 2, 12);
  const beyondLastPage = paginateLocally(recipes, 99, 12);

  assert.equal(secondPage.page, 2);
  assert.equal(secondPage.totalPages, 3);
  assert.equal(secondPage.totalCount, 25);
  assert.deepEqual(secondPage.items.map((item) => item.title), recipes.slice(12, 24).map((item) => item.title));
  assert.equal(beyondLastPage.page, 3);
  assert.equal(beyondLastPage.items.length, 1);
});
