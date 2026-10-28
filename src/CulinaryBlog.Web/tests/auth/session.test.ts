import assert from "node:assert/strict";
import test, { afterEach } from "node:test";
import { refreshTokenApi } from "../../lib/auth/auth-client.ts";
import { isAccountLocked, needsRefresh, refreshDelayMs, safeReturnUrl } from "../../lib/auth/session.ts";

const originalFetch = globalThis.fetch;

afterEach(() => {
  globalThis.fetch = originalFetch;
});

test("refreshDelayMs renews a 15-minute token one minute before it expires", () => {
  assert.equal(refreshDelayMs(900), 840_000);
});

test("refreshDelayMs never schedules a renewal sooner than five seconds", () => {
  assert.equal(refreshDelayMs(30), 5_000);
  assert.equal(refreshDelayMs(0), 5_000);
});

test("needsRefresh is true only inside the last minute before expiry", () => {
  const expiresAt = 1_000_000;
  assert.equal(needsRefresh(expiresAt, expiresAt - 61_000), false);
  assert.equal(needsRefresh(expiresAt, expiresAt - 60_000), true);
  assert.equal(needsRefresh(expiresAt, expiresAt + 1), true);
});

test("isAccountLocked reads the code from ProblemDetails.type as the API sends it", () => {
  assert.equal(isAccountLocked({ status: 401, type: "ACCOUNT_LOCKED" }), true);
  assert.equal(isAccountLocked({ status: 401, type: "INVALID_CREDENTIALS" }), false);
  assert.equal(isAccountLocked({ status: 401, type: "INVALID_GOOGLE_TOKEN" }), false);
});

test("isAccountLocked still accepts the legacy ACCOUNT_LOCKED_OUT code", () => {
  assert.equal(isAccountLocked({ status: 401, type: "ACCOUNT_LOCKED_OUT" }), true);
  assert.equal(isAccountLocked({ status: 401, errorCode: "ACCOUNT_LOCKED_OUT" }), true);
});

test("refreshTokenApi never sends two refresh requests at the same time", async () => {
  let inFlight = 0;
  let maxInFlight = 0;
  globalThis.fetch = async () => {
    inFlight++;
    maxInFlight = Math.max(maxInFlight, inFlight);
    await new Promise((resolve) => setTimeout(resolve, 20));
    inFlight--;
    return new Response(JSON.stringify({ accessToken: "a", expiresIn: 900, user: {} }), {
      status: 200,
      headers: { "Content-Type": "application/json" },
    });
  };

  await Promise.all([refreshTokenApi(), refreshTokenApi(), refreshTokenApi()]);

  assert.equal(maxInFlight, 1);
});

test("safeReturnUrl keeps same-site paths and rejects redirects off the site", () => {
  assert.equal(safeReturnUrl("/profile?tab=security"), "/profile?tab=security");
  assert.equal(safeReturnUrl(null), "/");
  assert.equal(safeReturnUrl(""), "/");
  assert.equal(safeReturnUrl("https://evil.example"), "/");
  assert.equal(safeReturnUrl("//evil.example"), "/");
  assert.equal(safeReturnUrl("/\\evil.example"), "/");
});
