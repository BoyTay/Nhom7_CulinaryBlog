import { ApiError } from "../api/client.ts";
import type {
  RecipeDetailDto,
  RecipeNutritionDto,
} from "./contracts.ts";
import { categoryHref } from "../navigation/routes.ts";
import { cookTimeLabel, difficultyLabel, formatDateVi, initials } from "../ui/format.ts";
import {
  preferredRecipeImageSource as selectRecipeImageSource,
  type RecipeImageSource,
} from "./image-display.ts";

export { preferredRecipeImageSource } from "./image-display.ts";

const uuidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

export interface RecipeDetailImageModel {
  id: string;
  image: RecipeImageSource;
  src: string;
  alt: string;
  isPrimary: boolean;
}

export interface RecipeDetailViewModel {
  id: string;
  title: string;
  description: string;
  categoryName: string;
  categoryHref: string;
  authorName: string;
  authorInitials: string;
  publishedLabel: string | null;
  difficulty: string;
  prepTime: string;
  cookTime: string;
  servings: string;
  ingredients: Array<{
    id: string;
    name: string;
    amount: string;
    notes?: string;
  }>;
  steps: Array<{
    id: string;
    number: number;
    description: string;
    timer?: string;
  }>;
  nutrition: Array<{ label: string; value: string }> | null;
  images: RecipeDetailImageModel[];
}

export function isRecipeId(value: string): boolean {
  return uuidPattern.test(value);
}

export function isPublicRecipeDetail(detail: RecipeDetailDto): boolean {
  return detail.status === "Published";
}

export function isRecipeNotFoundError(error: unknown): boolean {
  return error instanceof ApiError && error.status === 404;
}

export function createRecipeDetailViewModel(detail: RecipeDetailDto): RecipeDetailViewModel {
  const images = [...detail.images]
    .sort((left, right) => Number(right.isPrimary) - Number(left.isPrimary)
      || left.orderIndex - right.orderIndex)
    .flatMap((image) => {
      const src = selectRecipeImageSource(image);
      return src
        ? [{
            id: image.id,
            image,
            src,
            alt: image.altText?.trim() || detail.title,
            isPrimary: image.isPrimary,
          }]
        : [];
    });

  return {
    id: detail.id,
    title: detail.title,
    description: detail.description,
    categoryName: detail.categoryName,
    categoryHref: categoryHref(detail.categorySlug),
    authorName: detail.authorName,
    authorInitials: initials(detail.authorName),
    publishedLabel: detail.publishedAt ? `Đăng ngày ${formatDateVi(detail.publishedAt)}` : null,
    difficulty: difficultyLabel(detail.difficulty),
    prepTime: cookTimeLabel(detail.prepTimeMinutes),
    cookTime: cookTimeLabel(detail.cookTimeMinutes),
    servings: `${detail.servings} người`,
    ingredients: [...detail.ingredients]
      .sort((left, right) => left.sortOrder - right.sortOrder)
      .map((ingredient) => ({
        id: ingredient.id,
        name: ingredient.name,
        amount: `${ingredient.quantity.toLocaleString("vi-VN")} ${ingredient.unit}`,
        notes: ingredient.notes?.trim() || undefined,
      })),
    steps: [...detail.steps]
      .sort((left, right) => left.stepNumber - right.stepNumber)
      .map((step) => ({
        id: step.id,
        number: step.stepNumber,
        description: step.description,
        timer: step.timerMinutes === null ? undefined : cookTimeLabel(step.timerMinutes),
      })),
    nutrition: detail.nutrition ? nutritionItems(detail.nutrition) : null,
    images,
  };
}

function nutritionItems(nutrition: RecipeNutritionDto) {
  return [
    { label: "Năng lượng", value: `${nutrition.calories.toLocaleString("vi-VN")} kcal` },
    { label: "Đạm", value: `${nutrition.proteinGrams.toLocaleString("vi-VN")} g` },
    { label: "Carb", value: `${nutrition.carbohydratesGrams.toLocaleString("vi-VN")} g` },
    { label: "Béo", value: `${nutrition.fatGrams.toLocaleString("vi-VN")} g` },
  ];
}

export interface RecipeSeoModel {
  title: string;
  description: string;
  imageUrl?: string;
  jsonLd: Record<string, unknown>;
}

export function createRecipeSeoModel(detail: RecipeDetailDto): RecipeSeoModel {
  const imageUrls = [...detail.images]
    .sort((left, right) => Number(right.isPrimary) - Number(left.isPrimary)
      || left.orderIndex - right.orderIndex)
    .map((image) => selectRecipeImageSource(image))
    .filter((source): source is string => source !== null);
  const totalMinutes = detail.prepTimeMinutes + detail.cookTimeMinutes;
  const jsonLd: Record<string, unknown> = {
    "@context": "https://schema.org",
    "@type": "Recipe",
    name: detail.title,
    description: detail.description,
    dateCreated: detail.createdAt,
    prepTime: `PT${detail.prepTimeMinutes}M`,
    cookTime: `PT${detail.cookTimeMinutes}M`,
    totalTime: `PT${totalMinutes}M`,
    recipeYield: `${detail.servings} người`,
    author: { "@type": "Person", name: detail.authorName },
    recipeCategory: detail.categoryName,
    recipeIngredient: [...detail.ingredients]
      .sort((left, right) => left.sortOrder - right.sortOrder)
      .map((ingredient) => `${ingredient.quantity} ${ingredient.unit} ${ingredient.name}`),
    recipeInstructions: [...detail.steps]
      .sort((left, right) => left.stepNumber - right.stepNumber)
      .map((step) => ({
        "@type": "HowToStep",
        position: step.stepNumber,
        text: step.description,
        ...(step.imageUrl ? { image: step.imageUrl } : {}),
      })),
  };

  if (detail.publishedAt) jsonLd.datePublished = detail.publishedAt;
  if (imageUrls.length > 0) jsonLd.image = imageUrls;
  if (detail.nutrition) {
    jsonLd.nutrition = {
      "@type": "NutritionInformation",
      calories: `${detail.nutrition.calories} kcal`,
      proteinContent: `${detail.nutrition.proteinGrams} g`,
      carbohydrateContent: `${detail.nutrition.carbohydratesGrams} g`,
      fatContent: `${detail.nutrition.fatGrams} g`,
    };
  }

  return {
    title: `${detail.title} | Culinary Blog`,
    description: detail.description,
    imageUrl: imageUrls[0],
    jsonLd,
  };
}
