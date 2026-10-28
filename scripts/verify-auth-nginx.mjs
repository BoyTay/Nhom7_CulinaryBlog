#!/usr/bin/env node
// W08 Auth integration check through Nginx (issue #43). No dependencies; needs Node 22+.
//
//   node scripts/verify-auth-nginx.mjs
//
// Environment:
//   BASE_URL         default http://localhost     (the Nginx entry point, not the API container)
//   MAILHOG_URL      default http://localhost:8025
//   JOB_TIMEOUT_MS   default 60000                (welcome email is delivered by a Hangfire job)
//
// It registers throwaway users only. Each run makes 7 calls to /api/v1/auth, under the 10/minute per-IP limit;
// wait a minute between runs. Exit code is 1 if any step failed.
import { randomBytes } from "node:crypto";

const base = (process.env.BASE_URL ?? "http://localhost").replace(/\/$/, "");
const auth = `${base}/api/v1/auth`;
const origin = new URL(base).origin;
const mailhog = (process.env.MAILHOG_URL ?? "http://localhost:8025").replace(/\/$/, "");
const jobTimeout = Number(process.env.JOB_TIMEOUT_MS ?? 60_000);

const results = [];
const state = {};

async function step(name, body) {
  try {
    const detail = await body();
    results.push({ ok: true, name });
    console.log(`PASS  ${name}${detail ? ` — ${detail}` : ""}`);
  } catch (error) {
    results.push({ ok: false, name });
    console.log(`FAIL  ${name} — ${error.message}`);
  }
}

function expect(condition, message) {
  if (!condition) throw new Error(message);
}

function post(path, { cookie, bearer, body, from = origin } = {}) {
  const headers = { Accept: "application/json", Origin: from };
  if (body) headers["Content-Type"] = "application/json";
  if (cookie) headers.Cookie = `rt=${cookie}`;
  if (bearer) headers.Authorization = `Bearer ${bearer}`;
  return fetch(`${auth}${path}`, { method: "POST", headers, body: body ? JSON.stringify(body) : undefined });
}

function refreshCookie(response) {
  const header = response.headers.getSetCookie().find((value) => value.startsWith("rt="));
  expect(header, "no rt cookie in the response");
  return { header, value: decodeURIComponent(header.slice(3, header.indexOf(";"))) };
}

async function problemType(response) {
  return (await response.json().catch(() => ({}))).type;
}

const email = `w08-auth-${Date.now().toString(36)}@example.test`;

await step("Register through Nginx sets the refresh cookie with the ADR-0003 attributes", async () => {
  const response = await post("/register", {
    body: { displayName: "W08 Auth Check", email, password: `${randomBytes(12).toString("base64url")}-Aa1!` },
  });
  expect(response.status === 201, `status ${response.status}`);
  const { header, value } = refreshCookie(response);
  const attributes = header.toLowerCase();
  for (const required of ["httponly", "secure", "samesite=strict", "path=/api/v1/auth"]) {
    expect(attributes.includes(required), `cookie lacks ${required}: ${header}`);
  }
  expect(!attributes.includes("domain="), "cookie must stay host-only");
  const body = await response.json();
  state.access = body.accessToken;
  state.cookie = value;
  return `expiresIn=${body.expiresIn}s, roles=${body.user.roles.join(",")}`;
});

await step("Access token from Nginx is accepted by /auth/me", async () => {
  const response = await fetch(`${auth}/me`, { headers: { Authorization: `Bearer ${state.access}` } });
  expect(response.status === 200, `status ${response.status}`);
  const me = await response.json();
  expect(me.email === email && me.roles.includes("Author"), "unexpected profile or roles");
});

await step("Welcome email for the new account reaches MailHog (UserRegisteredEvent → Hangfire job)", async () => {
  const deadline = Date.now() + jobTimeout;
  while (Date.now() < deadline) {
    const response = await fetch(`${mailhog}/api/v2/search?kind=to&query=${encodeURIComponent(email)}`);
    const found = (await response.json()).items?.length ?? 0;
    if (found > 0) return `${found} message(s)`;
    await new Promise((resolve) => setTimeout(resolve, 1_000));
  }
  throw new Error(`no email within ${jobTimeout} ms (is Hangfire__Enabled=true?)`);
});

await step("Refresh with the cookie through Nginx rotates it", async () => {
  const response = await post("/refresh", { cookie: state.cookie });
  expect(response.status === 200, `status ${response.status}`);
  const { value } = refreshCookie(response);
  expect(value !== state.cookie, "cookie was not rotated");
  state.previousCookie = state.cookie;
  state.cookie = value;
  state.access = (await response.json()).accessToken;
});

await step("A foreign Origin cannot use the cookie (CSRF boundary behind the proxy)", async () => {
  const response = await post("/refresh", { cookie: state.cookie, from: "https://attacker.example" });
  expect(response.status === 403, `status ${response.status}`);
  return await problemType(response);
});

await step("Logout through Nginx expires the cookie", async () => {
  const response = await post("/logout", { cookie: state.cookie, bearer: state.access });
  expect(response.status === 204, `status ${response.status}`);
  const { header } = refreshCookie(response);
  expect(/expires=thu, 01 jan 1970/i.test(header), `cookie not expired: ${header}`);
});

await step("The logged-out cookie can no longer refresh", async () => {
  const response = await post("/refresh", { cookie: state.cookie });
  expect(response.status === 401, `status ${response.status}`);
  return await problemType(response);
});

await step("Replaying a rotated cookie is detected as reuse", async () => {
  const response = await post("/refresh", { cookie: state.previousCookie });
  expect(response.status === 401, `status ${response.status}`);
  const type = await problemType(response);
  expect(type === "REVOKED_REFRESH_TOKEN", `type ${type}`);
  return type;
});

const failed = results.filter((result) => !result.ok).length;
console.log(`\n${results.length - failed}/${results.length} passed`);
process.exit(failed ? 1 : 0);
