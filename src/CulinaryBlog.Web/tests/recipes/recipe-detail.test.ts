import assert from "node:assert/strict";
import test from "node:test";
import { ApiError } from "../../lib/api/client.ts";
import type { RecipeDetailDto, RecipeImageDto } from "../../lib/recipes/contracts.ts";
import {
  createRecipeDetailViewModel,
  createRecipeSeoModel,
  isPublicRecipeDetail,
  isRecipeId,
  isRecipeNotFoundError,
  preferredRecipeImageSource,
} from "../../lib/recipes/detail.ts";

function detail(overrides: Partial<RecipeDetailDto> = {}): RecipeDetailDto {
  return {
    id: "00000000-0000-4000-8000-000000000032",
    title: "Phở bò Hà Nội",
    slug: "pho-bo-ha-noi",
    description: "Một tô phở thơm với nước dùng trong.",
    categoryId: "11111111-1111-4111-8111-111111111111",
    authorId: "22222222-2222-4222-8222-222222222222",
    prepTimeMinutes: 30,
    cookTimeMinutes: 120,
    servings: 4,
    difficulty: "Medium",
    status: "Published",
    publishedAt: "2026-10-15T02:00:00Z",
    version: 3,
    nutrition: {
      calories: 450,
      proteinGrams: 30,
      carbohydratesGrams: 52,
      fatGrams: 12,
    },
    ingredients: [
      {
        id: "ingredient-2",
        name: "Bánh phở",
        quantity: 500,
        unit: "g",
        notes: null,
        sortOrder: 2,
      },
      {
        id: "ingredient-1",
        name: "Xương bò",
        quantity: 1.5,
        unit: "kg",
        notes: "rửa sạch",
        sortOrder: 1,
      },
    ],
    steps: [
      {
        id: "step-2",
        stepNumber: 2,
        description: "Trụng bánh phở và hoàn thiện.",
        timerMinutes: null,
        imageUrl: null,
      },
      {
        id: "step-1",
        stepNumber: 1,
        description: "Hầm xương lấy nước dùng.",
        timerMinutes: 120,
        imageUrl: "https://cdn.example.test/step-1.jpg",
      },
    ],
    images: [
      {
        id: "image-2",
        url: "https://cdn.example.test/image-2.jpg",
        originalUrl: "https://cdn.example.test/image-2-original.jpg",
        mediumUrl: null,
        thumbnailUrl: null,
        altText: null,
        isPrimary: false,
        orderIndex: 2,
      },
      {
        id: "image-1",
        url: "https://cdn.example.test/image-1.jpg",
        originalUrl: "https://cdn.example.test/image-1-original.jpg",
        mediumUrl: "https://cdn.example.test/image-1-medium.jpg",
        thumbnailUrl: "https://cdn.example.test/image-1-thumbnail.jpg",
        altText: "Tô phở bò",
        isPrimary: true,
        orderIndex: 1,
      },
    ],
    createdAt: "2026-10-14T02:00:00Z",
    categoryName: "Món nước",
    categorySlug: "mon-nuoc",
    authorName: "Phan Hiếu",
    ...overrides,
  };
}

test("populated detail maps actual API fields and preferred image variants", () => {
  const model = createRecipeDetailViewModel(detail());

  assert.equal(model.title, "Phở bò Hà Nội");
  assert.equal(model.difficulty, "Trung bình");
  assert.equal(model.prepTime, "30 phút");
  assert.equal(model.cookTime, "2 giờ");
  assert.equal(model.categoryName, "Món nước");
  assert.equal(model.categoryHref, "/categories/mon-nuoc");
  assert.equal(model.authorName, "Phan Hiếu");
  assert.equal(model.authorInitials, "PH");
  assert.equal(model.publishedLabel, "Đăng ngày 15/10/2026");
  assert.equal(model.servings, "4 người");
  assert.equal(model.images[0].id, "image-1");
  assert.equal(model.images[0].src, "https://cdn.example.test/image-1-medium.jpg");
  assert.equal(model.images[0].alt, "Tô phở bò");
  assert.equal(model.images[0].isPrimary, true);
  assert.equal(model.images[0].image.thumbnailUrl, "https://cdn.example.test/image-1-thumbnail.jpg");
  assert.equal(model.images[1].src, "https://cdn.example.test/image-2-original.jpg");
  assert.equal(model.images[1].alt, "Phở bò Hà Nội");
});

test("ingredients and steps preserve backend ordering fields", () => {
  const model = createRecipeDetailViewModel(detail());

  assert.deepEqual(model.ingredients.map((item) => item.name), ["Xương bò", "Bánh phở"]);
  assert.deepEqual(model.steps.map((item) => item.number), [1, 2]);
});

test("timer is present only when timerMinutes is returned", () => {
  const model = createRecipeDetailViewModel(detail());

  assert.equal(model.steps[0].timer, "2 giờ");
  assert.equal(model.steps[1].timer, undefined);
});

test("missing nutrition remains absent from the view and JSON-LD", () => {
  const recipe = detail({ nutrition: null });
  const model = createRecipeDetailViewModel(recipe);
  const seo = createRecipeSeoModel(recipe);

  assert.equal(model.nutrition, null);
  assert.equal("nutrition" in seo.jsonLd, false);
});

test("missing or unusable images select the detail placeholder path", () => {
  const emptyImage: RecipeImageDto = {
    id: "empty-image",
    url: " ",
    originalUrl: "",
    mediumUrl: null,
    thumbnailUrl: null,
    altText: null,
    isPrimary: true,
    orderIndex: 0,
  };

  assert.equal(preferredRecipeImageSource(emptyImage), null);
  assert.deepEqual(createRecipeDetailViewModel(detail({ images: [] })).images, []);
  assert.deepEqual(createRecipeDetailViewModel(detail({ images: [emptyImage] })).images, []);
});

test("404 and public visibility helpers protect the public route", () => {
  assert.equal(isRecipeId(detail().id), true);
  assert.equal(isRecipeId("pho-bo-ha-noi"), false);
  assert.equal(isPublicRecipeDetail(detail()), true);
  assert.equal(isPublicRecipeDetail(detail({ status: "Draft" })), false);
  assert.equal(isPublicRecipeDetail(detail({ status: "Archived" })), false);
  assert.equal(isRecipeNotFoundError(new ApiError(404, { title: "Not found" })), true);
  assert.equal(isRecipeNotFoundError(new ApiError(503, { title: "Unavailable" })), false);
});

test("SEO model uses real data, sorted content, author and category", () => {
  const seo = createRecipeSeoModel(detail());

  assert.equal(seo.title, "Phở bò Hà Nội | Culinary Blog");
  assert.equal(seo.description, "Một tô phở thơm với nước dùng trong.");
  assert.equal(seo.imageUrl, "https://cdn.example.test/image-1-medium.jpg");
  assert.deepEqual(seo.jsonLd.recipeIngredient, [
    "1.5 kg Xương bò",
    "500 g Bánh phở",
  ]);
  assert.deepEqual(
    (seo.jsonLd.recipeInstructions as Array<{ position: number }>).map((step) => step.position),
    [1, 2],
  );
  assert.deepEqual(seo.jsonLd.author, { "@type": "Person", name: "Phan Hiếu" });
  assert.equal(seo.jsonLd.recipeCategory, "Món nước");
  assert.deepEqual(seo.jsonLd.nutrition, {
    "@type": "NutritionInformation",
    calories: "450 kcal",
    proteinContent: "30 g",
    carbohydrateContent: "52 g",
    fatContent: "12 g",
  });
});

test("an unpublished recipe has no published label", () => {
  assert.equal(createRecipeDetailViewModel(detail({ publishedAt: null })).publishedLabel, null);
});
