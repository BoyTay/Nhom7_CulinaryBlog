import assert from "node:assert/strict";
import test from "node:test";
import { shouldDiscardCache } from "../../lib/auth/session.ts";

test("signing out discards what the signed-in user had cached", () => {
  assert.equal(shouldDiscardCache("user-a", null), true);
});

test("a different user signing in discards the previous user's cache", () => {
  assert.equal(shouldDiscardCache("user-a", "user-b"), true);
});

test("renewing the token of the same user keeps the cache", () => {
  assert.equal(shouldDiscardCache("user-a", "user-a"), false);
});

test("the first sign-in of a page load has nothing to discard", () => {
  assert.equal(shouldDiscardCache(null, "user-a"), false);
});

test("staying signed out has nothing to discard", () => {
  assert.equal(shouldDiscardCache(null, null), false);
});
