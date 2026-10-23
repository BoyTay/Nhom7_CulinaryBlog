#!/usr/bin/env node
// Cross-module smoke test for the Compose stack (issue #40). No dependencies; needs Node 22+.
//
//   node scripts/smoke-test.mjs
//
// Environment:
//   BASE_URL         default http://localhost
//   ADMIN_EMAIL      default admin@culinaryblog.local
//   ADMIN_PASSWORD   required; the development seed Admin password (Admin:Password in appsettings.Development.json)
//   MAILHOG_URL      default http://localhost:8025
//   MINIO_CONTAINER  default culinary-blog-minio-1   (docker exec is used to read sitemap.xml)
//   MINIO_BUCKET     default culinary-blog
//   MINIO_USER / MINIO_PASSWORD   defaults from compose.yaml
//   JOB_TIMEOUT_MS   default 90000
//
// The stack must run with Hangfire__Enabled=true (the compose default; HANGFIRE_ENABLED=false turns it off), otherwise the
// welcome-email, thumbnail and sitemap steps fail on purpose: disabled Hangfire silently drops events.
// Every step prints PASS/FAIL with the owning module; the exit code is 1 if any step failed.
import { execFileSync } from "node:child_process";
import { randomBytes } from "node:crypto";
import { deflateSync } from "node:zlib";

const base = (process.env.BASE_URL ?? "http://localhost").replace(/\/$/, "");
const api = `${base}/api/v1`;
const adminEmail = process.env.ADMIN_EMAIL ?? "admin@culinaryblog.local";
const adminPassword = process.env.ADMIN_PASSWORD;
if (!adminPassword) {
  console.error("ADMIN_PASSWORD is required (see Admin:Password in src/CulinaryBlog.API/appsettings.Development.json).");
  process.exit(2);
}
const mailhog = (process.env.MAILHOG_URL ?? "http://localhost:8025").replace(/\/$/, "");
const minioContainer = process.env.MINIO_CONTAINER ?? "culinary-blog-minio-1";
const bucket = process.env.MINIO_BUCKET ?? "culinary-blog";
const minioUser = process.env.MINIO_USER ?? "culinary";
const minioPassword = process.env.MINIO_PASSWORD ?? "change-me-for-local-development";
const jobTimeout = Number(process.env.JOB_TIMEOUT_MS ?? 90_000);

const run = Date.now().toString(36);
// Throwaway credential for the users this run registers: random, so nothing is stored in the repository.
function throwawayCredential() {
  const token = randomBytes(12).toString("base64url");
  return `${token}-Aa1!`;
}

const password = throwawayCredential();
const results = [];
const state = {};

async function step(owner, name, body) {
  try {
    const detail = await body();
    results.push({ ok: true, owner, name });
    console.log(`PASS  [${owner}] ${name}${detail ? ` — ${detail}` : ""}`);
  } catch (error) {
    results.push({ ok: false, owner, name, message: error.message });
    console.log(`FAIL  [${owner}] ${name} — ${error.message}`);
  }
}

function expect(condition, message) {
  if (!condition) throw new Error(message);
}

async function call(method, path, { token, headers = {}, json, body } = {}) {
  const init = { method, headers: { ...headers }, redirect: "manual" };
  if (token) init.headers.Authorization = `Bearer ${token}`;
  if (json !== undefined) {
    init.headers["Content-Type"] = "application/json";
    init.body = JSON.stringify(json);
  }
  if (body !== undefined) init.body = body;
  const response = await fetch(path.startsWith("http") ? path : `${api}${path}`, init);
  const text = await response.text();
  let data = null;
  try { data = text ? JSON.parse(text) : null; } catch { data = text; }
  return { status: response.status, data, etag: response.headers.get("etag"), headers: response.headers };
}

async function until(description, probe, timeoutMs = jobTimeout) {
  const deadline = Date.now() + timeoutMs;
  let last;
  while (Date.now() < deadline) {
    last = await probe();
    if (last) return last;
    await new Promise((resolve) => setTimeout(resolve, 2_000));
  }
  throw new Error(`timed out after ${Math.round(timeoutMs / 1000)}s waiting for ${description}`);
}

function png(size = 32) {
  const crcTable = Array.from({ length: 256 }, (_, n) => {
    let c = n;
    for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1;
    return c >>> 0;
  });
  const crc = (buffer) => {
    let c = 0xffffffff;
    for (const byte of buffer) c = crcTable[(c ^ byte) & 0xff] ^ (c >>> 8);
    return (c ^ 0xffffffff) >>> 0;
  };
  const chunk = (type, data) => {
    const length = Buffer.alloc(4); length.writeUInt32BE(data.length);
    const body = Buffer.concat([Buffer.from(type), data]);
    const sum = Buffer.alloc(4); sum.writeUInt32BE(crc(body));
    return Buffer.concat([length, body, sum]);
  };
  const header = Buffer.alloc(13);
  header.writeUInt32BE(size, 0); header.writeUInt32BE(size, 4); header[8] = 8; header[9] = 2;
  const rows = Buffer.alloc((size * 3 + 1) * size);
  for (let y = 0; y < size; y++) for (let x = 0; x < size * 3; x++) rows[y * (size * 3 + 1) + 1 + x] = (x * 7 + y * 3) & 0xff;
  return Buffer.concat([
    Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]),
    chunk("IHDR", header), chunk("IDAT", deflateSync(rows)), chunk("IEND", Buffer.alloc(0)),
  ]);
}

function readSitemap() {
  const script = `mc alias set smoke http://localhost:9000 ${minioUser} ${minioPassword} >/dev/null 2>&1 && mc cat smoke/${bucket}/sitemap.xml`;
  try {
    return execFileSync("docker", ["exec", minioContainer, "sh", "-c", script], { encoding: "utf8", stdio: ["ignore", "pipe", "ignore"] });
  } catch {
    return null;
  }
}

async function triggerSitemap() {
  const response = await call("POST", `${base}/hangfire/recurring/trigger`, {
    token: state.adminToken,
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body: "jobs[]=sitemap-daily",
  });
  expect(response.status === 204 || response.status === 200, `Hangfire trigger returned ${response.status}`);
}

async function sitemapContains(needle, wanted) {
  await triggerSitemap();
  return until(`sitemap.xml ${wanted ? "to contain" : "to omit"} ${needle}`, () => {
    const xml = readSitemap();
    return xml !== null && xml.includes(needle) === wanted ? xml : null;
  });
}

const title = `Phở bò w08smoke${run}`;
const slug = `pho-bo-w08smoke${run}`;
const categoryName = `Smoke ${run}`;
const authorEmail = `smoke-author-${run}@example.test`;
const otherEmail = `smoke-other-${run}@example.test`;

await step("Hiếu/ops", "readiness reports database, redis, minio and hangfire healthy", async () => {
  const response = await call("GET", `${base}/health/ready`);
  expect(response.status === 200, `ready returned ${response.status}: ${JSON.stringify(response.data)}`);
  const names = Object.keys(response.data.entries);
  expect(["database", "redis", "minio"].every((name) => names.includes(name)), `checks: ${names}`);
  expect(names.includes("hangfire"), "hangfire check missing — start the API with Hangfire__Enabled=true");
  return names.join(", ");
});

await step("Bo", "admin logs in and receives Admin role", async () => {
  const response = await call("POST", "/auth/login", { json: { email: adminEmail, password: adminPassword } });
  expect(response.status === 200, `login returned ${response.status}`);
  state.adminToken = response.data.accessToken;
  expect(response.data.user.roles.includes("Admin"), `roles: ${response.data.user.roles}`);
});

await step("Bo", "author registers, logs in and /auth/me returns the profile", async () => {
  const registered = await call("POST", "/auth/register", { json: { displayName: "Smoke Author", email: authorEmail, password } });
  expect(registered.status === 201 || registered.status === 200, `register returned ${registered.status}: ${JSON.stringify(registered.data)}`);
  const login = await call("POST", "/auth/login", { json: { email: authorEmail, password } });
  expect(login.status === 200, `login returned ${login.status}`);
  state.authorToken = login.data.accessToken;
  const me = await call("GET", "/auth/me", { token: state.authorToken });
  expect(me.status === 200 && me.data.email === authorEmail, `me returned ${me.status}`);
  state.authorId = me.data.id;
});

await step("Kiên/Bo", "welcome email for the new author reaches MailHog (Auth event → job)", async () => {
  const message = await until("welcome email", async () => {
    const response = await fetch(`${mailhog}/api/v2/search?kind=to&query=${encodeURIComponent(authorEmail)}`);
    const body = await response.json();
    return body.total > 0 ? body.items[0] : null;
  });
  return message.Content.Headers.Subject?.[0] ?? "message received";
});

await step("Hiếu", "admin creates a Category", async () => {
  const response = await call("POST", "/categories", { token: state.adminToken, json: { name: categoryName, description: "Smoke test", imageUrl: null, orderIndex: 99 } });
  expect(response.status === 201, `create returned ${response.status}: ${JSON.stringify(response.data)}`);
  state.category = response.data;
});

await step("Trường", "author creates a Recipe with a step and an ingredient and publishes it", async () => {
  const created = await call("POST", "/recipes", { token: state.authorToken, json: {
    title, slug, description: "Món kiểm thử tích hợp.", categoryId: state.category.id,
    prepTimeMinutes: 10, cookTimeMinutes: 30, servings: 2, difficulty: 0 } });
  expect(created.status === 201, `create returned ${created.status}: ${JSON.stringify(created.data)}`);
  state.recipeId = created.data.id;
  let detail = await call("GET", `/recipes/${state.recipeId}`, { token: state.authorToken });
  expect(detail.etag, "detail returned no ETag");
  const step = await call("POST", `/recipes/${state.recipeId}/steps`, { token: state.authorToken, headers: { "If-Match": detail.etag }, json: { description: "Ninh xương.", timerMinutes: 20, imageUrl: null } });
  expect(step.status === 201 || step.status === 200, `add step returned ${step.status}: ${JSON.stringify(step.data)}`);
  detail = await call("GET", `/recipes/${state.recipeId}`, { token: state.authorToken });
  const ingredient = await call("POST", `/recipes/${state.recipeId}/ingredients`, { token: state.authorToken, headers: { "If-Match": detail.etag }, json: { name: "Bánh phở", quantity: 200, unit: "g", notes: null } });
  expect(ingredient.status === 201 || ingredient.status === 200, `add ingredient returned ${ingredient.status}: ${JSON.stringify(ingredient.data)}`);
  detail = await call("GET", `/recipes/${state.recipeId}`, { token: state.authorToken });
  const published = await call("PATCH", `/recipes/${state.recipeId}/publish`, { token: state.authorToken, headers: { "If-Match": detail.etag } });
  expect(published.status === 200 || published.status === 204, `publish returned ${published.status}: ${JSON.stringify(published.data)}`);
});

await step("Trường/Kiên", "image upload succeeds and the thumbnail job fills the thumbnail URL", async () => {
  const detail = await call("GET", `/recipes/${state.recipeId}`, { token: state.authorToken });
  const form = new FormData();
  form.append("file", new Blob([png()], { type: "image/png" }), "smoke.png");
  form.append("altText", "Smoke image");
  const upload = await call("POST", `/recipes/${state.recipeId}/images`, { token: state.authorToken, headers: { "If-Match": detail.etag }, body: form });
  expect(upload.status === 201, `upload returned ${upload.status}: ${JSON.stringify(upload.data)}`);
  const image = await until("thumbnailUrl", async () => {
    const current = await call("GET", `/recipes/${state.recipeId}`, { token: state.authorToken });
    return current.data.images?.find((item) => item.thumbnailUrl) ?? null;
  });
  return image.thumbnailUrl;
});

await step("Kiên", "search finds the recipe with and without diacritics", async () => {
  for (const query of [`Phở bò w08smoke${run}`, `pho bo w08smoke${run}`]) {
    const found = await until(`search for "${query}"`, async () => {
      const response = await call("GET", `/recipes/search?q=${encodeURIComponent(query)}`);
      return response.status === 200 && response.data.items.some((item) => item.id === state.recipeId) ? response : null;
    }, 15_000);
    expect(found.status === 200, "search failed");
  }
});

await step("Hiếu/Trường", "Category detail lists the published recipe", async () => {
  const response = await call("GET", `/categories/${state.category.slug}`);
  expect(response.status === 200, `detail returned ${response.status}`);
  expect(response.data.recipes.items.some((item) => item.id === state.recipeId), "recipe not listed in its category");
});

await step("Kiên", "sitemap.xml lists the published recipe", async () => {
  await sitemapContains(`/recipes/${state.recipeId}`, true);
});

await step("Bo/Trường", "another author cannot update the recipe (403)", async () => {
  await call("POST", "/auth/register", { json: { displayName: "Smoke Other", email: otherEmail, password } });
  const login = await call("POST", "/auth/login", { json: { email: otherEmail, password } });
  expect(login.status === 200, `other login returned ${login.status}`);
  const detail = await call("GET", `/recipes/${state.recipeId}`, { token: login.data.accessToken });
  const update = await call("PUT", `/recipes/${state.recipeId}`, { token: login.data.accessToken, headers: { "If-Match": detail.etag },
    json: { title: "Hijack", slug: `hijack-${run}`, description: "x", categoryId: state.category.id, prepTimeMinutes: 1, cookTimeMinutes: 1, servings: 1, difficulty: 0 } });
  expect(update.status === 403, `update returned ${update.status}`);
});

await step("Trường", "owner soft-deletes the recipe", async () => {
  const detail = await call("GET", `/recipes/${state.recipeId}`, { token: state.authorToken });
  const response = await call("DELETE", `/recipes/${state.recipeId}`, { token: state.authorToken, headers: { "If-Match": detail.etag } });
  expect(response.status === 204, `delete returned ${response.status}`);
});

await step("Kiên/Trường", "deleted recipe is absent from search and Category detail", async () => {
  const search = await call("GET", `/recipes/search?q=${encodeURIComponent(`w08smoke${run}`)}`);
  expect(!search.data.items.some((item) => item.id === state.recipeId), "deleted recipe still in search");
  const detail = await call("GET", `/categories/${state.category.slug}`);
  expect(!detail.data.recipes.items.some((item) => item.id === state.recipeId), "deleted recipe still in category");
  const direct = await call("GET", `/recipes/${state.recipeId}`);
  expect(direct.status === 404, `direct GET returned ${direct.status}`);
});

await step("Kiên", "sitemap.xml no longer lists the deleted recipe", async () => {
  await sitemapContains(`/recipes/${state.recipeId}`, false);
});

await step("Hiếu", "cleanup: admin deletes the smoke Category", async () => {
  const response = await call("DELETE", `/categories/${state.category.id}`, { token: state.adminToken });
  expect(response.status === 204, `delete returned ${response.status}: ${JSON.stringify(response.data)}`);
});

const failed = results.filter((result) => !result.ok);
console.log(`\n${results.length - failed.length}/${results.length} steps passed`);
if (failed.length > 0) {
  console.log("Failures by owner:");
  for (const result of failed) console.log(`  [${result.owner}] ${result.name}: ${result.message}`);
  process.exit(1);
}
