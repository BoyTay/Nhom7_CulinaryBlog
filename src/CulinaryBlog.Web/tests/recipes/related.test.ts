import assert from "node:assert/strict";
import test from "node:test";
import type { RecipeListItemDto, RecipeStatus } from "../../lib/recipes/contracts.ts";
import { pickRelated } from "../../lib/recipes/related.ts";

function recipe(index: number, over: Partial<RecipeListItemDto> = {}): RecipeListItemDto {
  return {
    id: `id-${index}`,
    title: `Món ${index}`,
    slug: `mon-${index}`,
    description: "",
    categoryId: "c1",
    authorId: "a1",
    prepTimeMinutes: 5,
    cookTimeMinutes: 20,
    servings: 2,
    difficulty: "Easy",
    status: "Published" as RecipeStatus,
    createdAt: new Date(Date.UTC(2026, 9, index)).toISOString(),
    categoryName: "Món nước",
    categorySlug: "mon-nuoc",
    authorName: "A",
    thumbnailUrl: null,
    updatedAt: null,
    ...over,
  };
}

test("pickRelated keeps published recipes of the same category, newest first, without the current one", () => {
  const all = [
    recipe(1),
    recipe(2, { categoryId: "c2" }),
    recipe(3, { status: "Draft" }),
    recipe(4),
    recipe(5),
    recipe(6, { categoryId: "c2" }),
  ];

  assert.deepEqual(pickRelated(all, { id: "id-1", categoryId: "c1" }).map((r) => r.id), ["id-5", "id-4"]);
});

test("pickRelated honours the limit and returns nothing when the category is alone", () => {
  const all = [recipe(1), recipe(2), recipe(3), recipe(4), recipe(5)];
  assert.equal(pickRelated(all, { id: "id-1", categoryId: "c1" }, 3).length, 3);
  assert.deepEqual(pickRelated([recipe(1)], { id: "id-1", categoryId: "c1" }), []);
});
