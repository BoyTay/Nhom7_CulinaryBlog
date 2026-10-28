import assert from "node:assert/strict";
import test from "node:test";
import { canAccessAdmin } from "../../lib/navigation/access.ts";

test("only the Admin role can access admin pages, ignoring case", () => {
  assert.equal(canAccessAdmin(["Admin"]), true);
  assert.equal(canAccessAdmin(["author", "ADMIN"]), true);
});

test("authors, unknown roles and missing roles cannot access admin pages", () => {
  assert.equal(canAccessAdmin(["Author"]), false);
  assert.equal(canAccessAdmin(["Moderator"]), false);
  assert.equal(canAccessAdmin([]), false);
  assert.equal(canAccessAdmin(undefined), false);
});
