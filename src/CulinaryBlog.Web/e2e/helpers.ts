import { randomBytes } from "node:crypto";
import * as zlib from "node:zlib";
import { expect, type APIRequestContext, type Page } from "@playwright/test";

const api = "/api/v1";

export interface TestAuthor {
  email: string;
  password: string;
  token: string;
}

export interface CreatedRecipe {
  id: string;
  slug: string;
  title: string;
}

/** Registers a throwaway author through the API; credentials are random so nothing sensitive lives in the repo. */
export async function registerAuthor(request: APIRequestContext): Promise<TestAuthor> {
  const suffix = randomBytes(5).toString("hex");
  const email = `e2e-${suffix}@example.test`;
  const password = `${randomBytes(12).toString("base64url")}-Aa1!`;
  const response = await request.post(`${api}/auth/register`, {
    data: { displayName: `E2E ${suffix}`, email, password },
  });
  expect(response.status(), "register").toBe(201);
  const body = (await response.json()) as { accessToken: string };
  return { email, password, token: body.accessToken };
}

export async function loginThroughUi(page: Page, author: Pick<TestAuthor, "email" | "password">): Promise<void> {
  await page.locator("#em").fill(author.email);
  await page.locator("#pw").fill(author.password);
  await page.locator("button[type=submit]").click();
  // Wait until the sign-in finished and the app left the login screen, otherwise a following goto aborts it.
  await page.waitForURL((url) => !url.pathname.startsWith("/login"));
}

async function firstCategory(request: APIRequestContext): Promise<{ id: string; slug: string; name: string }> {
  const response = await request.get(`${api}/categories`);
  const categories = (await response.json()) as Array<{ id: string; slug: string; name: string }>;
  return categories.find((category) => category.slug === "mon-chinh") ?? categories[0];
}

function authorHeaders(token: string, etag?: string): Record<string, string> {
  return { Authorization: `Bearer ${token}`, ...(etag ? { "If-Match": etag } : {}) };
}

async function currentEtag(request: APIRequestContext, token: string, id: string): Promise<string> {
  const response = await request.get(`${api}/recipes/${id}`, { headers: authorHeaders(token) });
  const etag = response.headers()["etag"];
  expect(etag, "detail ETag").toBeTruthy();
  return etag;
}

/** Creates a draft; with `complete` it also gets a step and an ingredient so it can be published. */
export async function createDraftRecipe(
  request: APIRequestContext,
  author: TestAuthor,
  options: { complete: boolean; label: string },
): Promise<CreatedRecipe> {
  const category = await firstCategory(request);
  const suffix = randomBytes(4).toString("hex");
  const title = `${options.label} ${suffix}`;
  const slug = `${options.label.toLowerCase().replace(/[^a-z0-9]+/g, "-")}-${suffix}`;

  const created = await request.post(`${api}/recipes`, {
    headers: authorHeaders(author.token),
    data: {
      title,
      slug,
      description: "Công thức kiểm thử đầu cuối.",
      categoryId: category.id,
      prepTimeMinutes: 10,
      cookTimeMinutes: 30,
      servings: 2,
      difficulty: 0,
    },
  });
  expect(created.status(), "create recipe").toBe(201);
  const { id } = (await created.json()) as { id: string };

  if (options.complete) {
    const step = await request.post(`${api}/recipes/${id}/steps`, {
      headers: authorHeaders(author.token, await currentEtag(request, author.token, id)),
      data: { description: "Ninh xương trong hai giờ.", timerMinutes: 20, imageUrl: null },
    });
    expect(step.status(), "create step").toBe(201);
    const ingredient = await request.post(`${api}/recipes/${id}/ingredients`, {
      headers: authorHeaders(author.token, await currentEtag(request, author.token, id)),
      data: { name: "Bánh phở", quantity: 200, unit: "g", notes: null },
    });
    expect(ingredient.status(), "create ingredient").toBe(201);
  }

  return { id, slug, title };
}

export async function deleteRecipe(request: APIRequestContext, author: TestAuthor, id: string): Promise<void> {
  await request.delete(`${api}/recipes/${id}`, {
    headers: authorHeaders(author.token, await currentEtag(request, author.token, id)),
  });
}

/** A valid solid-colour PNG, built in memory so tests need no fixture files. */
export function createPng(width: number, height: number): Buffer {
  const crc = (buffer: Buffer) => {
    const out = Buffer.alloc(4);
    out.writeUInt32BE(zlib.crc32(buffer) >>> 0);
    return out;
  };
  const chunk = (type: string, data: Buffer) => {
    const length = Buffer.alloc(4);
    length.writeUInt32BE(data.length);
    const body = Buffer.concat([Buffer.from(type, "ascii"), data]);
    return Buffer.concat([length, body, crc(body)]);
  };
  const header = Buffer.alloc(13);
  header.writeUInt32BE(width, 0);
  header.writeUInt32BE(height, 4);
  header[8] = 8; // bit depth
  header[9] = 2; // RGB
  const row = Buffer.concat([Buffer.from([0]), Buffer.alloc(width * 3, 0x66)]);
  const raw = Buffer.concat(Array.from({ length: height }, () => row));
  return Buffer.concat([
    Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]),
    chunk("IHDR", header),
    chunk("IDAT", zlib.deflateSync(raw)),
    chunk("IEND", Buffer.alloc(0)),
  ]);
}
