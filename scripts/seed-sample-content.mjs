#!/usr/bin/env node
// Seeds real sample content (categories, Vietnamese recipes, photos) through the public API.
// Photos come from Wikimedia Commons under free licences; the script also writes docs/SAMPLE_CONTENT_CREDITS.md.
//
//   node scripts/seed-sample-content.mjs
//
// Environment:
//   BASE_URL              default http://localhost
//   ADMIN_EMAIL           default admin@culinaryblog.local
//   ADMIN_PASSWORD        required (Admin:Password in src/CulinaryBlog.API/appsettings.Development.json)
//   SEED_AUTHOR_EMAIL     default bep-nha@culinaryblog.local (the account that owns the recipes)
//   SEED_AUTHOR_PASSWORD  optional; a random one is generated and printed once when the account is created
//
// Safe to re-run: existing categories and recipes (matched by name or slug) are skipped.
import { randomBytes } from "node:crypto";
import { readFileSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const root = join(dirname(fileURLToPath(import.meta.url)), "..");
const data = JSON.parse(readFileSync(join(root, "scripts", "seed-data", "sample-recipes.json"), "utf8"));
const base = (process.env.BASE_URL ?? "http://localhost").replace(/\/$/, "");
const api = `${base}/api/v1`;
const adminEmail = process.env.ADMIN_EMAIL ?? "admin@culinaryblog.local";
const adminPassword = process.env.ADMIN_PASSWORD;
const authorEmail = process.env.SEED_AUTHOR_EMAIL ?? "bep-nha@culinaryblog.local";
if (!adminPassword) {
  console.error("ADMIN_PASSWORD is required.");
  process.exit(2);
}

const commonsApi = "https://commons.wikimedia.org/w/api.php";
const userAgent = "CulinaryBlogStudentProject/1.0 (course project; sample content seeding)";
const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

async function call(method, path, { token, etag, json, body } = {}) {
  const headers = { Accept: "application/json" };
  if (token) headers.Authorization = `Bearer ${token}`;
  if (etag) headers["If-Match"] = etag;
  if (json) headers["Content-Type"] = "application/json";
  const response = await fetch(`${api}${path}`, { method, headers, body: json ? JSON.stringify(json) : body });
  const text = await response.text();
  let parsed = null;
  try {
    parsed = text ? JSON.parse(text) : null;
  } catch {
    parsed = text;
  }
  return { status: response.status, data: parsed, etag: response.headers.get("etag") };
}

function expect(result, ok, what) {
  const accepted = Array.isArray(ok) ? ok : [ok];
  if (!accepted.includes(result.status)) {
    throw new Error(`${what} failed: ${result.status} ${JSON.stringify(result.data)}`);
  }
  return result;
}

async function commons(params, attempt = 0) {
  const response = await fetch(`${commonsApi}?${new URLSearchParams({ format: "json", ...params })}`, {
    headers: { "User-Agent": userAgent },
  });
  const text = await response.text();
  if (response.status === 429 || text.startsWith("You are making too many requests")) {
    if (attempt >= 5) throw new Error("Wikimedia Commons keeps rate limiting this client.");
    await sleep(8000 * (attempt + 1));
    return commons(params, attempt + 1);
  }
  return JSON.parse(text);
}

const stripTags = (value) => (value ?? "").replace(/<[^>]+>/g, "").replace(/&amp;/g, "&").replace(/\s+/g, " ").trim();

/** Resolves a Commons file title to a 1280px download plus licence and author details. */
async function resolveImage(title) {
  const json = await commons({
    action: "query",
    titles: title,
    prop: "imageinfo",
    iiprop: "url|mime|extmetadata",
    iiurlwidth: "1280",
  });
  const page = Object.values(json.query.pages)[0];
  const info = page?.imageinfo?.[0];
  if (!info) throw new Error(`Commons file not found: ${title}`);
  const meta = info.extmetadata ?? {};
  return {
    title,
    url: info.thumburl ?? info.url,
    mime: info.mime,
    pageUrl: info.descriptionurl,
    author: stripTags(meta.Artist?.value) || "Không rõ tác giả",
    license: meta.LicenseShortName?.value ?? "Giấy phép tự do",
    licenseUrl: meta.LicenseUrl?.value ?? "",
  };
}

async function download(url, attempt = 0) {
  const response = await fetch(url, { headers: { "User-Agent": userAgent } });
  if (response.status === 429 && attempt < 5) {
    await sleep(8000 * (attempt + 1));
    return download(url, attempt + 1);
  }
  if (!response.ok) throw new Error(`Download failed ${response.status}: ${url}`);
  return { bytes: await response.arrayBuffer(), type: response.headers.get("content-type") ?? "image/jpeg" };
}

const credits = [];

// 1. Admin creates or completes the categories.
const adminLogin = expect(await call("POST", "/auth/login", { json: { email: adminEmail, password: adminPassword } }), 200, "admin login");
const adminToken = adminLogin.data.accessToken;
const existing = (await call("GET", "/categories")).data;
const categoryByName = new Map();
for (const [index, wanted] of data.categories.entries()) {
  const found = existing.find((category) => category.name === wanted.name);
  if (!found) {
    const created = expect(
      await call("POST", "/categories", { token: adminToken, json: { name: wanted.name, description: wanted.description, imageUrl: null, orderIndex: index } }),
      201,
      `create category ${wanted.name}`,
    );
    categoryByName.set(wanted.name, created.data);
    console.log(`+ category ${wanted.name}`);
  } else {
    if (!found.description) {
      expect(
        await call("PUT", `/categories/${found.id}`, { token: adminToken, json: { name: found.name, description: wanted.description, imageUrl: found.imageUrl, orderIndex: found.orderIndex } }),
        200,
        `update category ${wanted.name}`,
      );
      console.log(`~ category ${wanted.name} (description)`);
    }
    categoryByName.set(wanted.name, found);
  }
}

// 2. The author account that owns the sample recipes.
let authorPassword = process.env.SEED_AUTHOR_PASSWORD;
let authorToken;
if (authorPassword) {
  const login = await call("POST", "/auth/login", { json: { email: authorEmail, password: authorPassword } });
  if (login.status === 200) authorToken = login.data.accessToken;
}
if (!authorToken) {
  authorPassword = authorPassword ?? `${randomBytes(12).toString("base64url")}-Aa1!`;
  const registered = expect(
    await call("POST", "/auth/register", { json: { displayName: "Bếp Nhà Culinary", email: authorEmail, password: authorPassword } }),
    [200, 201],
    "register seed author",
  );
  authorToken = registered.data.accessToken;
  console.log(`Created author ${authorEmail}. Password (shown once, keep it somewhere safe): ${authorPassword}`);
}

// 3. Recipes with steps, ingredients, a photo and publishing.
const currentEtag = async (id) => (await call("GET", `/recipes/${id}`, { token: authorToken })).etag;

for (const recipe of data.recipes) {
  if ((await call("GET", `/recipes/${recipe.slug}`)).status === 200) {
    console.log(`= ${recipe.slug} already published, skipped`);
    continue;
  }
  const category = categoryByName.get(recipe.category);
  if (!category) throw new Error(`Unknown category ${recipe.category} for ${recipe.slug}`);

  const created = expect(
    await call("POST", "/recipes", {
      token: authorToken,
      json: {
        title: recipe.title,
        slug: recipe.slug,
        description: recipe.description,
        categoryId: category.id,
        prepTimeMinutes: recipe.prepTimeMinutes,
        cookTimeMinutes: recipe.cookTimeMinutes,
        servings: recipe.servings,
        difficulty: recipe.difficulty,
      },
    }),
    201,
    `create ${recipe.slug}`,
  );
  const id = created.data.id;

  for (const ingredient of recipe.ingredients) {
    expect(await call("POST", `/recipes/${id}/ingredients`, { token: authorToken, etag: await currentEtag(id), json: ingredient }), 201, `ingredient ${ingredient.name}`);
  }
  for (const step of recipe.steps) {
    expect(
      await call("POST", `/recipes/${id}/steps`, { token: authorToken, etag: await currentEtag(id), json: { description: step.description, timerMinutes: step.timerMinutes, imageUrl: null } }),
      201,
      `step of ${recipe.slug}`,
    );
  }

  const image = await resolveImage(recipe.image);
  await sleep(1200);
  const file = await download(image.url);
  const form = new FormData();
  form.append("file", new Blob([file.bytes], { type: file.type }), `${recipe.slug}.jpg`);
  form.append("altText", `${recipe.title}. Ảnh: ${image.author}, ${image.license}, Wikimedia Commons`.slice(0, 200));
  form.append("isPrimary", "true");
  expect(await call("POST", `/recipes/${id}/images`, { token: authorToken, etag: await currentEtag(id), body: form }), [200, 201], `image of ${recipe.slug}`);

  expect(await call("PATCH", `/recipes/${id}/publish`, { token: authorToken, etag: await currentEtag(id) }), [200, 204], `publish ${recipe.slug}`);
  credits.push({ recipe: recipe.title, slug: recipe.slug, ...image });
  console.log(`+ ${recipe.slug} (${image.license})`);
  await sleep(1200);
}

// 4. Credits file required by the CC BY / CC BY-SA licences.
if (credits.length > 0) {
  const lines = [
    "# Nguồn ảnh của nội dung mẫu",
    "",
    "Ảnh món ăn trong dữ liệu mẫu lấy từ Wikimedia Commons theo giấy phép tự do. Danh sách này do `scripts/seed-sample-content.mjs` tạo ra.",
    "Ảnh được thu nhỏ về chiều rộng 1280px; không chỉnh sửa nội dung.",
    "",
    "| Công thức | Tệp trên Commons | Tác giả | Giấy phép |",
    "| --- | --- | --- | --- |",
    ...credits.map((item) => `| ${item.recipe} | [${item.title.replace("File:", "")}](${item.pageUrl}) | ${item.author} | ${item.licenseUrl ? `[${item.license}](${item.licenseUrl})` : item.license} |`),
    "",
  ];
  writeFileSync(join(root, "docs", "SAMPLE_CONTENT_CREDITS.md"), lines.join("\n"), "utf8");
  console.log(`Wrote docs/SAMPLE_CONTENT_CREDITS.md (${credits.length} images)`);
}
console.log("Done.");
