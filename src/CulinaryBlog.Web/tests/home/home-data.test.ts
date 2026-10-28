import assert from "node:assert/strict";
import test from "node:test";
import type { CategoryDto } from "../../lib/api/contracts.ts";
import { buildHomeViewModel } from "../../lib/home/home-data.ts";
import type { RecipeListItemDto, RecipeStatus } from "../../lib/recipes/contracts.ts";

function recipe(index: number, status: RecipeStatus = "Published"): RecipeListItemDto {
  return {
    id: `00000000-0000-4000-8000-${String(index).padStart(12, "0")}`,
    title: `Món ${index}`,
    slug: `mon-${index}`,
    description: "",
    categoryId: "c1",
    authorId: "a1",
    prepTimeMinutes: 5,
    cookTimeMinutes: 10,
    servings: 2,
    difficulty: "Easy",
    status,
    createdAt: new Date(Date.UTC(2026, 9, index)).toISOString(),
    categoryName: "Món nước",
    categorySlug: "mon-nuoc",
    authorName: "Phan Hiếu",
    thumbnailUrl: null,
    updatedAt: null,
  };
}

function category(name: string, recipeCount: number): CategoryDto {
  return { id: name, name, slug: name.toLowerCase(), description: null, imageUrl: null, orderIndex: 0, recipeCount };
}

const ok = { recipes: false, categories: false };

test("featured recipes are the six newest published ones", () => {
  const recipes = [...Array.from({ length: 8 }, (_, i) => recipe(i + 1)), recipe(9, "Draft")];
  const model = buildHomeViewModel(recipes, [], ok);

  assert.equal(model.featured.length, 6);
  assert.deepEqual(model.featured.map((card) => card.title), ["Món 8", "Món 7", "Món 6", "Món 5", "Món 4", "Món 3"]);
  assert.equal(model.state, "ok");
});

test("categories without recipes are hidden", () => {
  const model = buildHomeViewModel([], [category("Chay", 3), category("Rỗng", 0)], ok);
  assert.deepEqual(model.categories.map((c) => c.name), ["Chay"]);
  assert.equal(model.categories[0].href, "/categories/chay");
});

test("a recipe failure with healthy categories is partial and keeps categories", () => {
  const model = buildHomeViewModel([], [category("Chay", 3)], { recipes: true, categories: false });
  assert.equal(model.state, "partial");
  assert.equal(model.featured.length, 0);
  assert.equal(model.categories.length, 1);
});

test("both sources failing is an error state", () => {
  const model = buildHomeViewModel([], [], { recipes: true, categories: true });
  assert.equal(model.state, "error");
});
