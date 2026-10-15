import assert from "node:assert/strict";
import test from "node:test";
import { ApiError } from "../../lib/api/client.ts";
import type { RecipeClient } from "../../lib/recipes/client.ts";
import type { RecipeDetailDto, RecipeImageDto } from "../../lib/recipes/contracts.ts";
import {
  deleteRecipeImageAndRefresh,
  MAX_RECIPE_IMAGE_BYTES,
  RecipeImageDeletedRefreshError,
  RecipeImageUploadCancelledError,
  setPrimaryRecipeImage,
  uploadRecipeImageWithProgress,
  validateRecipeImageFile,
  type RecipeImageXhr,
} from "../../lib/recipes/images.ts";

const image: RecipeImageDto = {
  id: "image-1",
  url: "https://images.test/image-1.jpg",
  originalUrl: "https://images.test/image-1-original.jpg",
  mediumUrl: "https://images.test/image-1-medium.jpg",
  thumbnailUrl: "https://images.test/image-1-thumb.jpg",
  altText: "Finished recipe",
  isPrimary: true,
  orderIndex: 0,
};

class FakeXhr implements RecipeImageXhr {
  upload = { onprogress: null as ((event: ProgressEvent) => void) | null };
  status = 0;
  responseText = "";
  withCredentials = false;
  onload: (() => void) | null = null;
  onerror: (() => void) | null = null;
  onabort: (() => void) | null = null;
  method = "";
  url = "";
  body: FormData | null = null;
  headers = new Map<string, string>();
  responseHeaders = new Map<string, string>();
  aborted = false;

  open(method: string, url: string) {
    this.method = method;
    this.url = url;
  }

  setRequestHeader(name: string, value: string) {
    this.headers.set(name.toLowerCase(), value);
  }

  getResponseHeader(name: string) {
    return this.responseHeaders.get(name.toLowerCase()) ?? null;
  }

  send(body: FormData) {
    this.body = body;
  }

  abort() {
    this.aborted = true;
    this.onabort?.();
  }
}

function uploadFile(size = 128, type = "image/webp") {
  return new File([new Uint8Array(size)], "recipe.webp", { type });
}

test("image validation accepts supported formats through exactly 5 MiB", () => {
  for (const type of ["image/jpeg", "image/png", "image/webp", "image/avif"]) {
    assert.equal(validateRecipeImageFile({ name: "recipe", size: MAX_RECIPE_IMAGE_BYTES, type }), null);
  }
  assert.match(validateRecipeImageFile({ name: "recipe.gif", size: 10, type: "image/gif" })!, /JPEG/);
  assert.match(validateRecipeImageFile({ name: "empty.png", size: 0, type: "image/png" })!, /trống/);
  assert.match(validateRecipeImageFile({ name: "large.png", size: MAX_RECIPE_IMAGE_BYTES + 1, type: "image/png" })!, /5 MiB/);
});

test("XHR upload sends multipart fields, auth and If-Match without setting Content-Type", async () => {
  const xhr = new FakeXhr();
  const progress: number[] = [];
  const task = uploadRecipeImageWithProgress({
    recipeId: "recipe/1",
    file: uploadFile(),
    altText: "  Món ăn hoàn chỉnh  ",
    isPrimary: true,
    eTag: '"8"',
    accessToken: "access-token",
    baseUrl: "https://api.test/api/v1",
    onProgress: (value) => progress.push(value),
    xhrFactory: () => xhr,
  });

  xhr.upload.onprogress?.({ lengthComputable: true, loaded: 25, total: 100 } as ProgressEvent);
  xhr.status = 201;
  xhr.responseText = JSON.stringify(image);
  xhr.responseHeaders.set("etag", '"9"');
  xhr.onload?.();

  const result = await task.promise;
  assert.equal(xhr.method, "POST");
  assert.equal(xhr.url, "https://api.test/api/v1/recipes/recipe%2F1/images");
  assert.equal(xhr.withCredentials, true);
  assert.equal(xhr.headers.get("authorization"), "Bearer access-token");
  assert.equal(xhr.headers.get("if-match"), '"8"');
  assert.equal(xhr.headers.has("content-type"), false);
  assert.equal(xhr.body?.get("altText"), "Món ăn hoàn chỉnh");
  assert.equal(xhr.body?.get("isPrimary"), "true");
  assert.ok(xhr.body?.get("file") instanceof Blob);
  assert.deepEqual(progress, [25, 100]);
  assert.equal(result.eTag, '"9"');
  assert.deepEqual(result.data, image);
});

test("XHR upload exposes RFC 7807 failures", async () => {
  const xhr = new FakeXhr();
  const task = uploadRecipeImageWithProgress({
    recipeId: "recipe-1",
    file: uploadFile(),
    altText: "",
    isPrimary: false,
    eTag: '"4"',
    accessToken: "token",
    xhrFactory: () => xhr,
  });
  xhr.status = 412;
  xhr.responseText = JSON.stringify({ type: "RECIPE_CONCURRENCY_CONFLICT", title: "Recipe changed." });
  xhr.onload?.();

  await assert.rejects(task.promise, (error: unknown) => {
    assert.ok(error instanceof ApiError);
    assert.equal(error.status, 412);
    assert.equal(error.problem.type, "RECIPE_CONCURRENCY_CONFLICT");
    return true;
  });
});

test("XHR upload can be cancelled while preserving a retryable caller state", async () => {
  const xhr = new FakeXhr();
  const task = uploadRecipeImageWithProgress({
    recipeId: "recipe-1",
    file: uploadFile(),
    altText: "",
    isPrimary: false,
    eTag: '"4"',
    accessToken: "token",
    xhrFactory: () => xhr,
  });
  task.cancel();
  assert.equal(xhr.aborted, true);
  await assert.rejects(task.promise, RecipeImageUploadCancelledError);
});

test("set primary sends the current ETag and replaces it with the response ETag", async () => {
  const second = { ...image, id: "image-2", isPrimary: false };
  let sentEtag = "";
  const client = {
    async setPrimaryImage(_recipeId: string, imageId: string, eTag: string) {
      assert.equal(imageId, "image-2");
      sentEtag = eTag;
      return { data: { ...second, isPrimary: true }, eTag: '"11"' };
    },
  } as unknown as RecipeClient;

  const result = await setPrimaryRecipeImage(client, "recipe-1", "image-2", '"10"', [image, second]);
  assert.equal(sentEtag, '"10"');
  assert.equal(result.eTag, '"11"');
  assert.deepEqual(result.images.map((item) => item.isPrimary), [false, true]);
});

test("delete refetches detail because delete does not return a fresh ETag", async () => {
  const calls: string[] = [];
  const detail = { images: [] } as unknown as RecipeDetailDto;
  const client = {
    async deleteImage(_recipeId: string, imageId: string, eTag: string) {
      calls.push(`delete:${imageId}:${eTag}`);
      return { data: undefined, eTag: null };
    },
    async detail(recipeId: string) {
      calls.push(`detail:${recipeId}`);
      return { data: detail, eTag: '"13"' };
    },
  } as unknown as RecipeClient;

  const result = await deleteRecipeImageAndRefresh(client, "recipe-1", "image-1", '"12"');
  assert.deepEqual(calls, ['delete:image-1:"12"', "detail:recipe-1"]);
  assert.equal(result.eTag, '"13"');
  assert.deepEqual(result.images, []);
});

test("delete reports the post-delete refresh boundary so further stale writes can stop", async () => {
  const client = {
    async deleteImage() {
      return { data: undefined, eTag: null };
    },
    async detail() {
      throw new Error("network down");
    },
  } as unknown as RecipeClient;

  await assert.rejects(
    deleteRecipeImageAndRefresh(client, "recipe-1", "image-1", '"12"'),
    (error: unknown) => error instanceof RecipeImageDeletedRefreshError && error.imageId === "image-1",
  );
});
