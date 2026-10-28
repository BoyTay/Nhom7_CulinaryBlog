import assert from "node:assert/strict";
import test from "node:test";
import type { CategoryDto, PagedResult } from "../../lib/api/contracts.ts";
import {
  buildCategoryDetailView,
  parseCategoryPage,
  type CategoryRecipeSummaryDto,
} from "../../lib/categories/category-view.ts";

const category: CategoryDto = {
  id: "c1",
  name: "Món chay",
  slug: "mon-chay",
  description: "Món không thịt",
  imageUrl: null,
  orderIndex: 0,
  recipeCount: 2,
};

function summary(index: number): CategoryRecipeSummaryDto {
  return {
    id: `id-${index}`,
    title: `Đậu hũ ${index}`,
    slug: `dau-hu-${index}`,
    description: "Mô tả",
    createdAt: "2026-10-01T00:00:00Z",
    cookTimeMinutes: 45,
    difficulty: "Easy",
    thumbnailUrl: null,
  };
}

function paged(items: CategoryRecipeSummaryDto[], totalCount: number, page = 1): PagedResult<CategoryRecipeSummaryDto> {
  const pageSize = 12;
  const totalPages = totalCount === 0 ? 0 : Math.ceil(totalCount / pageSize);
  return { items, totalCount, page, pageSize, totalPages, hasNextPage: page < totalPages, hasPreviousPage: page > 1 };
}

test("parseCategoryPage defaults to 1 and rejects non-positive or non-numeric values", () => {
  assert.equal(parseCategoryPage(undefined), 1);
  assert.equal(parseCategoryPage("0"), 1);
  assert.equal(parseCategoryPage("-2"), 1);
  assert.equal(parseCategoryPage("abc"), 1);
  assert.equal(parseCategoryPage("1.5"), 1);
  assert.equal(parseCategoryPage("3"), 3);
});

test("a category without recipes yields the empty state", () => {
  const view = buildCategoryDetailView(category, paged([], 0));
  assert.equal(view.state, "empty");
  assert.equal(view.cards.length, 0);
});

test("recipes become shared recipe cards with time, difficulty and thumbnail", () => {
  const withPhoto = { ...summary(1), thumbnailUrl: "https://img.test/1.webp", difficulty: "Hard" as const };
  const view = buildCategoryDetailView(category, paged([withPhoto, summary(2)], 25));

  assert.equal(view.state, "populated");
  assert.deepEqual(view.cards[0], {
    id: "id-1",
    href: "/recipes/dau-hu-1",
    title: "Đậu hũ 1",
    categoryName: "Món chay",
    categoryHref: "/categories/mon-chay",
    cookTimeLabel: "45 phút",
    difficultyLabel: "Khó",
    thumbnailUrl: "https://img.test/1.webp",
    thumbnailAlt: "Ảnh món Đậu hũ 1",
  });
  assert.equal(view.cards[1].difficultyLabel, "Dễ");
  assert.equal(view.totalPages, 3);
  assert.equal(view.title, "Món chay");
});

test("a category without a description falls back to its recipe count", () => {
  const view = buildCategoryDetailView({ ...category, description: null }, paged([summary(1)], 7));
  assert.equal(view.subtitle, "7 công thức");
  assert.equal(buildCategoryDetailView(category, paged([summary(1)], 7)).subtitle, "Món không thịt");
});
