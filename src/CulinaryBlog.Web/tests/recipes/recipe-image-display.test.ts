import assert from "node:assert/strict";
import test from "node:test";
import { createRecipeImageRemotePatterns } from "../../lib/recipes/image-config.ts";
import {
  RECIPE_IMAGE_PLACEHOLDER,
  preferredRecipeImageSource,
  recipeImageSourceCandidates,
  recipeImageSourceForAttempt,
} from "../../lib/recipes/image-display.ts";
import type { RecipeImageDto } from "../../lib/recipes/contracts.ts";

const image: RecipeImageDto = {
  id: "image-1",
  thumbnailUrl: "https://cdn.test/thumbnail.webp",
  mediumUrl: "https://cdn.test/medium.webp",
  originalUrl: "https://cdn.test/original.webp",
  url: "https://cdn.test/legacy.webp",
  altText: "Món ăn hoàn chỉnh",
  isPrimary: true,
  orderIndex: 0,
};

test("thumbnail rendering falls back through medium, original, then legacy URL", () => {
  assert.deepEqual(recipeImageSourceCandidates(image, "thumbnail"), [
    "https://cdn.test/thumbnail.webp",
    "https://cdn.test/medium.webp",
    "https://cdn.test/original.webp",
    "https://cdn.test/legacy.webp",
  ]);
});

test("main rendering prefers medium and safely falls back without duplicate requests", () => {
  assert.equal(preferredRecipeImageSource(image), "https://cdn.test/medium.webp");
  assert.deepEqual(recipeImageSourceCandidates({
    ...image,
    mediumUrl: " ",
    originalUrl: image.url,
  }), ["https://cdn.test/legacy.webp", "https://cdn.test/thumbnail.webp"]);
});

test("missing sources resolve to the checked-in local placeholder", () => {
  assert.equal(RECIPE_IMAGE_PLACEHOLDER, "/images/recipe-placeholder.svg");
  assert.equal(preferredRecipeImageSource(null), null);
  assert.deepEqual(recipeImageSourceCandidates(undefined), []);
});

test("a broken remote chain ends on the local placeholder without another remote host", () => {
  const candidates = recipeImageSourceCandidates(image, "thumbnail");
  assert.deepEqual(recipeImageSourceForAttempt(candidates, 1), {
    src: "https://cdn.test/medium.webp",
    isPlaceholder: false,
  });
  assert.deepEqual(recipeImageSourceForAttempt(candidates, candidates.length), {
    src: RECIPE_IMAGE_PLACEHOLDER,
    isPlaceholder: true,
  });
});

test("remote patterns allow exact local MinIO and optional CDN origins", () => {
  assert.deepEqual(createRecipeImageRemotePatterns({
    minioEndpoint: "http://localhost:9000",
    cdnEndpoint: "https://cdn.culinary.example/recipes/",
  }), [
    { protocol: "http", hostname: "localhost", port: "9000", pathname: "/**" },
    { protocol: "https", hostname: "cdn.culinary.example", port: "", pathname: "/recipes/**" },
  ]);
});

test("remote patterns reject wildcard hosts and unsafe endpoint forms", () => {
  for (const endpoint of [
    "https://*.example.com",
    "ftp://cdn.example.com",
    "https://user:secret@cdn.example.com",
    "https://cdn.example.com/images?token=secret",
  ]) {
    assert.throws(
      () => createRecipeImageRemotePatterns({ cdnEndpoint: endpoint }),
      TypeError,
    );
  }
});
