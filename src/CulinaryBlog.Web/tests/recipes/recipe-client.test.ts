import assert from "node:assert/strict";
import test from "node:test";
import { ApiError } from "../../lib/api/client.ts";
import { createRecipeClient, responseEtag } from "../../lib/recipes/client.ts";
import type { UpdateRecipeRequest } from "../../lib/recipes/contracts.ts";

const updateRequest: UpdateRecipeRequest = {
  title: "Pho bo",
  slug: "pho-bo",
  description: "Traditional noodle soup.",
  categoryId: "11111111-1111-4111-8111-111111111111",
  prepTimeMinutes: 20,
  cookTimeMinutes: 120,
  servings: 6,
  difficulty: "Medium",
  calories: null,
  proteinGrams: null,
  carbohydratesGrams: null,
  fatGrams: null,
};

test("responseEtag preserves the quoted HTTP ETag", () => {
  assert.equal(responseEtag(new Headers({ ETag: '"42"' })), '"42"');
  assert.equal(responseEtag(new Headers()), null);
});

test("detail returns its response body together with the ETag header", async () => {
  const client = createRecipeClient({
    baseUrl: "https://api.example.test/api/v1",
    fetchImplementation: async () => new Response(JSON.stringify({ id: "recipe-1", version: 7 }), {
      status: 200,
      headers: { "Content-Type": "application/json", ETag: '"7"' },
    }),
  });

  const result = await client.detail("recipe-1");
  assert.equal(result.data.id, "recipe-1");
  assert.equal(result.data.version, 7);
  assert.equal(result.eTag, '"7"');
});

test("ETag-sensitive update sends If-Match and auth headers and captures the next ETag", async () => {
  let capturedUrl = "";
  let capturedInit: RequestInit | undefined;
  const client = createRecipeClient({
    baseUrl: "https://api.example.test/api/v1",
    getAccessToken: () => "access-token",
    fetchImplementation: async (input, init) => {
      capturedUrl = input.toString();
      capturedInit = init;
      return new Response(JSON.stringify({ id: "recipe-1", version: 9 }), {
        status: 200,
        headers: { "Content-Type": "application/json", ETag: '"9"' },
      });
    },
  });

  const result = await client.update("recipe-1", updateRequest, '"8"');
  const headers = new Headers(capturedInit?.headers);

  assert.equal(capturedUrl, "https://api.example.test/api/v1/recipes/recipe-1");
  assert.equal(capturedInit?.method, "PUT");
  assert.equal(capturedInit?.credentials, "include");
  assert.equal(headers.get("If-Match"), '"8"');
  assert.equal(headers.get("Authorization"), "Bearer access-token");
  assert.equal(headers.get("Content-Type"), "application/json");
  assert.deepEqual(JSON.parse(capturedInit?.body as string), updateRequest);
  assert.equal(result.eTag, '"9"');
});

test("nested mutation sends the current If-Match value", async () => {
  let headers = new Headers();
  const client = createRecipeClient({
    fetchImplementation: async (_input, init) => {
      headers = new Headers(init?.headers);
      return new Response(JSON.stringify({ id: "ingredient-1" }), {
        status: 201,
        headers: { "Content-Type": "application/json", ETag: '"13"' },
      });
    },
  });

  const result = await client.createIngredient(
    "recipe-1",
    { name: "Beef", quantity: 300, unit: "g", notes: null },
    '"12"',
  );

  assert.equal(headers.get("If-Match"), '"12"');
  assert.equal(result.eTag, '"13"');
});

test("image upload uses the backend multipart field names without overriding its content type", async () => {
  let capturedInit: RequestInit | undefined;
  const client = createRecipeClient({
    fetchImplementation: async (_input, init) => {
      capturedInit = init;
      return new Response(JSON.stringify({ id: "image-1", originalUrl: "https://images.test/image.png" }), {
        status: 201,
        headers: { "Content-Type": "application/json", ETag: '"14"' },
      });
    },
  });

  const result = await client.uploadImage(
    "recipe-1",
    new Blob([new Uint8Array([1, 2, 3])], { type: "image/png" }),
    "recipe.png",
    "Recipe photo",
    true,
    '"13"',
  );
  const headers = new Headers(capturedInit?.headers);
  const formData = capturedInit?.body as FormData;

  assert.equal(headers.get("Content-Type"), null);
  assert.equal(headers.get("If-Match"), '"13"');
  assert.equal(formData.get("altText"), "Recipe photo");
  assert.equal(formData.get("isPrimary"), "true");
  assert.ok(formData.get("file") instanceof Blob);
  assert.equal(result.eTag, '"14"');
});

test("RFC 7807 concurrency and precondition failures remain available through ApiError", async () => {
  for (const problem of [
    { status: 422, type: "RECIPE_CONCURRENCY_CONFLICT", title: "Recipe was changed." },
    { status: 428, type: "PRECONDITION_REQUIRED", title: "If-Match header is required" },
  ]) {
    const client = createRecipeClient({
      fetchImplementation: async () => new Response(JSON.stringify(problem), {
        status: problem.status,
        headers: { "Content-Type": "application/problem+json" },
      }),
    });

    await assert.rejects(
      client.update("recipe-1", updateRequest, '"1"'),
      (error: unknown) => {
        assert.ok(error instanceof ApiError);
        assert.equal(error.status, problem.status);
        assert.equal(error.problem.type, problem.type);
        assert.equal(error.problem.title, problem.title);
        return true;
      },
    );
  }
});

test("204 responses return undefined data and preserve any response ETag", async () => {
  const client = createRecipeClient({
    fetchImplementation: async () => new Response(null, {
      status: 204,
      headers: { ETag: '"15"' },
    }),
  });

  const result = await client.deleteStep("recipe-1", "step-1", '"14"');
  assert.equal(result.data, undefined);
  assert.equal(result.eTag, '"15"');
});

test("detailBySlug requests the slug URL and returns the body with its ETag", async () => {
  let requestedUrl = "";
  const client = createRecipeClient({
    baseUrl: "https://api.example.test/api/v1",
    fetchImplementation: async (input) => {
      requestedUrl = String(input);
      return new Response(JSON.stringify({ id: "recipe-1", slug: "pho bo", version: 3 }), {
        status: 200,
        headers: { "Content-Type": "application/json", ETag: '"3"' },
      });
    },
  });

  const result = await client.detailBySlug("pho bo");

  assert.equal(requestedUrl, "https://api.example.test/api/v1/recipes/pho%20bo");
  assert.equal(result.data.slug, "pho bo");
  assert.equal(result.eTag, '"3"');
});

test("the client-level cache option is applied to requests unless a call overrides it", async () => {
  const seen: Array<RequestCache | undefined> = [];
  const fetchImplementation: typeof fetch = async (_input, init) => {
    seen.push(init?.cache);
    return new Response(JSON.stringify([]), { status: 200, headers: { "Content-Type": "application/json" } });
  };

  await createRecipeClient({ baseUrl: "https://api.example.test/api/v1", fetchImplementation, cache: "force-cache" }).list();
  await createRecipeClient({ baseUrl: "https://api.example.test/api/v1", fetchImplementation }).list();

  assert.deepEqual(seen, ["force-cache", "no-store"]);
});
