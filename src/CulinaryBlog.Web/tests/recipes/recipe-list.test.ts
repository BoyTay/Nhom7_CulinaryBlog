import assert from "node:assert/strict";
import test from "node:test";
import type { RecipeListItemDto } from "../../lib/recipes/contracts.ts";
import {
  createRecipeListViewModel,
  toRecipeCardModel,
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
    categoryName: "Món nước",
    categorySlug: "mon-nuoc",
    authorName: "Phan Hiếu",
    thumbnailUrl: null,
    updatedAt: null,
  };
}

test("cards carry the category, time, difficulty and thumbnail shown in the design", () => {
  const model = createRecipeListViewModel([recipe(1), { ...recipe(2), thumbnailUrl: "https://img.test/t.webp" }]);

  assert.equal(model.state, "populated");
  assert.equal(model.totalCount, 2);
  if (model.state !== "populated") return;
  assert.deepEqual(model.cards[0], {
    id: recipe(1).id,
    href: `/recipes/${recipe(1).slug}`,
    title: "Recipe 1",
    categoryName: "Món nước",
    categoryHref: "/categories/mon-nuoc",
    cookTimeLabel: "21 phút",
    difficultyLabel: "Trung bình",
    thumbnailUrl: null,
    thumbnailAlt: "Ảnh món Recipe 1",
  });
  assert.equal(model.cards[1].thumbnailUrl, "https://img.test/t.webp");
});

test("toRecipeCardModel keeps long titles intact for the card to clamp", () => {
  const long = "Phở ".repeat(60).trim();
  assert.equal(toRecipeCardModel({ ...recipe(1), title: long }).title, long);
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
