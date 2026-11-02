import assert from "node:assert/strict";
import test, { afterEach } from "node:test";
import { ApiError } from "../../lib/api/client.ts";
import { authStore } from "../../lib/auth/auth-store.ts";

afterEach(() => {
  authStore.clearAccessToken();
  authStore.consumeAccountLocked();
});

function countLockReports(): { count: () => number; stop: () => void } {
  let reports = 0;
  const stop = authStore.onAccountLocked(() => {
    reports += 1;
  });
  return { count: () => reports, stop };
}

test("an ACCOUNT_LOCKED answer during a session reports the lock once", () => {
  authStore.setAccessToken("token");
  const reports = countLockReports();

  new ApiError(401, { status: 401, type: "ACCOUNT_LOCKED" });

  assert.equal(reports.count(), 1);
  assert.equal(authStore.consumeAccountLocked(), true);
  assert.equal(authStore.consumeAccountLocked(), false);
  reports.stop();
});

test("a failed sign-in has no session to end, so the login form stays in charge", () => {
  const reports = countLockReports();

  new ApiError(401, { status: 401, type: "ACCOUNT_LOCKED" });

  assert.equal(reports.count(), 0);
  assert.equal(authStore.consumeAccountLocked(), false);
  reports.stop();
});

test("other 401 answers are not a lock", () => {
  authStore.setAccessToken("token");
  const reports = countLockReports();

  new ApiError(401, { status: 401, type: "INVALID_CREDENTIALS" });
  new ApiError(401, { status: 401 });

  assert.equal(reports.count(), 0);
  reports.stop();
});
