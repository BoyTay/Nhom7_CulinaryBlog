import type { RecipeImageDto } from "./contracts.ts";

export const RECIPE_IMAGE_PLACEHOLDER = "/images/recipe-placeholder.svg";

export type RecipeImageVariant = "thumbnail" | "medium";
export type RecipeImageSource = Pick<
  RecipeImageDto,
  "thumbnailUrl" | "mediumUrl" | "originalUrl" | "url"
>;

function usable(value: string | null | undefined): string | null {
  const normalized = value?.trim();
  return normalized || null;
}

export function recipeImageSourceCandidates(
  image: RecipeImageSource | null | undefined,
  variant: RecipeImageVariant = "medium",
): string[] {
  if (!image) return [];
  const ordered = variant === "thumbnail"
    ? [image.thumbnailUrl, image.mediumUrl, image.originalUrl, image.url]
    : [image.mediumUrl, image.originalUrl, image.url, image.thumbnailUrl];
  return ordered
    .map(usable)
    .filter((source, index, sources): source is string => Boolean(source) && sources.indexOf(source) === index);
}

export function preferredRecipeImageSource(
  image: RecipeImageSource | null | undefined,
  variant: RecipeImageVariant = "medium",
): string | null {
  return recipeImageSourceCandidates(image, variant)[0] ?? null;
}

export function recipeImageSourceForAttempt(
  candidates: readonly string[],
  failedAttempts: number,
): { src: string; isPlaceholder: boolean } {
  const src = candidates[Math.max(0, failedAttempts)];
  return src
    ? { src, isPlaceholder: false }
    : { src: RECIPE_IMAGE_PLACEHOLDER, isPlaceholder: true };
}
