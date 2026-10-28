import assert from "node:assert/strict";
import test from "node:test";
import { isActiveNav } from "../../lib/navigation/routes.ts";

test("isActiveNav matches a section and its children", () => {
  assert.equal(isActiveNav("/recipes", "/recipes"), true);
  assert.equal(isActiveNav("/recipes/pho-bo", "/recipes"), true);
  assert.equal(isActiveNav("/categories/mon-chay", "/categories"), true);
  assert.equal(isActiveNav("/dashboard/recipes", "/dashboard"), true);
});

test("isActiveNav keeps home exact and never matches prefixes of other words", () => {
  assert.equal(isActiveNav("/", "/"), true);
  assert.equal(isActiveNav("/recipes", "/"), false);
  assert.equal(isActiveNav("/recipes-archive", "/recipes"), false);
  assert.equal(isActiveNav("/search", "/recipes"), false);
});
