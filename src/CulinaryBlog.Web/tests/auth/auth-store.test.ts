import assert from "node:assert/strict";
import test, { beforeEach } from "node:test";
import { authStore } from "../../lib/auth/auth-store.ts";

beforeEach(() => {
  authStore.clearAccessToken();
});

test("authStore starts with null token", () => {
  assert.strictEqual(authStore.getAccessToken(), null);
});

test("authStore sets and gets access token in memory", () => {
  const sampleToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.sample";
  authStore.setAccessToken(sampleToken);
  assert.strictEqual(authStore.getAccessToken(), sampleToken);
});

test("authStore clears access token", () => {
  authStore.setAccessToken("token-to-be-cleared");
  assert.strictEqual(authStore.getAccessToken(), "token-to-be-cleared");

  authStore.clearAccessToken();
  assert.strictEqual(authStore.getAccessToken(), null);
});

test("authStore notifies subscribers on token change", () => {
  const changes: (string | null)[] = [];
  const unsubscribe = authStore.onTokenChange((token) => {
    changes.push(token);
  });

  authStore.setAccessToken("token-1");
  authStore.setAccessToken("token-2");
  authStore.clearAccessToken();

  unsubscribe();

  authStore.setAccessToken("token-3-ignored");

  assert.deepStrictEqual(changes, ["token-1", "token-2", null]);
});

test("authStore respects ADR-0003: does not use localStorage or sessionStorage", () => {
  // Simulate browser environment without Web Storage writes
  let storageAccessed = false;
  const mockStorage = {
    setItem: () => {
      storageAccessed = true;
    },
    getItem: () => {
      storageAccessed = true;
      return null;
    },
    removeItem: () => {
      storageAccessed = true;
    },
  };

  (globalThis as unknown as { localStorage?: typeof mockStorage }).localStorage = mockStorage;
  (globalThis as unknown as { sessionStorage?: typeof mockStorage }).sessionStorage = mockStorage;

  try {
    authStore.setAccessToken("secure-memory-token");
    assert.strictEqual(authStore.getAccessToken(), "secure-memory-token");
    authStore.clearAccessToken();
    assert.strictEqual(storageAccessed, false, "ADR-0003 violated: Web Storage was touched!");
  } finally {
    delete (globalThis as unknown as { localStorage?: typeof mockStorage }).localStorage;
    delete (globalThis as unknown as { sessionStorage?: typeof mockStorage }).sessionStorage;
  }
});
