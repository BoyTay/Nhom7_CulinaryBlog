import assert from "node:assert/strict";
import test, { afterEach, beforeEach } from "node:test";
import {
  authFetch,
  loginApi,
  logoutApi,
  registerApi,
  updateProfileApi,
} from "../../lib/auth/auth-client.ts";
import { authStore } from "../../lib/auth/auth-store.ts";
import { ApiError } from "../../lib/api/client.ts";

const originalFetch = globalThis.fetch;

beforeEach(() => {
  authStore.clearAccessToken();
});

afterEach(() => {
  globalThis.fetch = originalFetch;
});

test("authFetch sends credentials: include and Accept: application/json", async () => {
  let capturedInit: RequestInit | undefined;

  globalThis.fetch = async (_input: string | URL | Request, init?: RequestInit) => {
    capturedInit = init;
    return new Response(JSON.stringify({ ok: true }), {
      status: 200,
      headers: { "Content-Type": "application/json" },
    });
  };

  const result = await authFetch<{ ok: boolean }>("/test-endpoint");
  assert.strictEqual(result.ok, true);
  assert.strictEqual(capturedInit?.credentials, "include");

  const headers = new Headers(capturedInit?.headers);
  assert.strictEqual(headers.get("Accept"), "application/json");
  assert.strictEqual(headers.get("Authorization"), null);
});

test("authFetch attaches Bearer token when token exists in authStore", async () => {
  authStore.setAccessToken("my-secret-jwt-token");
  let capturedHeaders: Headers | undefined;

  globalThis.fetch = async (_input: string | URL | Request, init?: RequestInit) => {
    capturedHeaders = new Headers(init?.headers);
    return new Response(JSON.stringify({ user: { id: "u-1" } }), {
      status: 200,
      headers: { "Content-Type": "application/json" },
    });
  };

  await authFetch("/auth/me");
  assert.strictEqual(capturedHeaders?.get("Authorization"), "Bearer my-secret-jwt-token");
});

test("authFetch returns undefined for 204 No Content", async () => {
  globalThis.fetch = async () => {
    return new Response(null, { status: 204 });
  };

  const result = await authFetch<void>("/auth/logout", { method: "POST" });
  assert.strictEqual(result, undefined);
});

test("authFetch throws ApiError with ApiProblemDetails on error status", async () => {
  globalThis.fetch = async () => {
    return new Response(
      JSON.stringify({
        status: 401,
        title: "Unauthorized",
        detail: "Email hoặc mật khẩu không chính xác.",
        errorCode: "INVALID_CREDENTIALS",
      }),
      {
        status: 401,
        headers: { "Content-Type": "application/problem+json" },
      },
    );
  };

  await assert.rejects(
    async () => {
      await authFetch("/auth/login", { method: "POST" });
    },
    (err: unknown) => {
      assert.ok(err instanceof ApiError);
      assert.strictEqual(err.status, 401);
      assert.strictEqual(err.problem.errorCode, "INVALID_CREDENTIALS");
      assert.strictEqual(err.problem.detail, "Email hoặc mật khẩu không chính xác.");
      return true;
    },
  );
});

test("loginApi sends POST request with JSON credentials", async () => {
  let capturedMethod: string | undefined;
  let capturedBody: string | undefined;

  globalThis.fetch = async (_url, init) => {
    capturedMethod = init?.method;
    capturedBody = init?.body as string;
    return new Response(
      JSON.stringify({
        accessToken: "login-jwt",
        expiresIn: 900,
        user: { id: "u-123", email: "test@example.com", displayName: "Tester", roles: ["User"] },
      }),
      { status: 200, headers: { "Content-Type": "application/json" } },
    );
  };

  const response = await loginApi({ email: "test@example.com", password: "Password123!" });
  assert.strictEqual(capturedMethod, "POST");
  assert.deepStrictEqual(JSON.parse(capturedBody || "{}"), {
    email: "test@example.com",
    password: "Password123!",
  });
  assert.strictEqual(response.accessToken, "login-jwt");
  assert.strictEqual(response.user.displayName, "Tester");
});

test("registerApi sends POST request with registration payload", async () => {
  let capturedBody: string | undefined;

  globalThis.fetch = async (_url, init) => {
    capturedBody = init?.body as string;
    return new Response(
      JSON.stringify({
        accessToken: "reg-jwt",
        expiresIn: 900,
        user: { id: "u-456", email: "new@example.com", displayName: "Newbie", roles: ["User"] },
      }),
      { status: 200, headers: { "Content-Type": "application/json" } },
    );
  };

  const response = await registerApi({
    displayName: "Newbie",
    email: "new@example.com",
    password: "Password123!",
  });
  assert.strictEqual(response.accessToken, "reg-jwt");
  assert.deepStrictEqual(JSON.parse(capturedBody || "{}"), {
    displayName: "Newbie",
    email: "new@example.com",
    password: "Password123!",
  });
});

test("updateProfileApi sends PATCH request with profile fields", async () => {
  let capturedMethod: string | undefined;
  let capturedBody: string | undefined;

  globalThis.fetch = async (_url, init) => {
    capturedMethod = init?.method;
    capturedBody = init?.body as string;
    return new Response(
      JSON.stringify({
        id: "u-123",
        email: "test@example.com",
        displayName: "Updated Name",
        avatarUrl: "https://example.com/avatar.jpg",
        bio: "Chef",
        roles: ["Chef"],
      }),
      { status: 200, headers: { "Content-Type": "application/json" } },
    );
  };

  const updated = await updateProfileApi({
    displayName: "Updated Name",
    avatarUrl: "https://example.com/avatar.jpg",
    bio: "Chef",
  });

  assert.strictEqual(capturedMethod, "PATCH");
  assert.deepStrictEqual(JSON.parse(capturedBody || "{}"), {
    displayName: "Updated Name",
    avatarUrl: "https://example.com/avatar.jpg",
    bio: "Chef",
  });
  assert.strictEqual(updated.displayName, "Updated Name");
});

test("logoutApi sends POST to /auth/logout", async () => {
  let capturedUrl: string | undefined;
  let capturedMethod: string | undefined;

  globalThis.fetch = async (url, init) => {
    capturedUrl = url.toString();
    capturedMethod = init?.method;
    return new Response(null, { status: 204 });
  };

  await logoutApi();
  assert.strictEqual(capturedMethod, "POST");
  assert.ok(capturedUrl?.includes("/auth/logout"));
});

