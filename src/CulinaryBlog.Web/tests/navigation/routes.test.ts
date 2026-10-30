import assert from "node:assert/strict";
import test from "node:test";
import { categoryHref, isKnownRoute, recipeHref, routes } from "../../lib/navigation/routes.ts";

test("recipeHref and categoryHref build slug URLs and escape unsafe characters", () => {
  assert.equal(recipeHref("pho-bo"), "/recipes/pho-bo");
  assert.equal(categoryHref("mon-chay"), "/categories/mon-chay");
  assert.equal(recipeHref("a/b"), "/recipes/a%2Fb");
});

test("isKnownRoute accepts registry paths and dynamic detail paths and ignores the query", () => {
  assert.equal(isKnownRoute(routes.dashboardRecipes), true);
  assert.equal(isKnownRoute("/recipes/pho-bo?x=1"), true);
  assert.equal(isKnownRoute("/dashboard/recipes/abc/edit"), true);
  assert.equal(isKnownRoute("/nope"), false);
});

test("recipeDetailHref links public recipe pages by slug, not id", async () => {
  const { recipeDetailHref } = await import("../../lib/navigation/routes.ts");
  assert.equal(recipeDetailHref({ id: "0000-1", slug: "pho-bo" }), "/recipes/pho-bo");
});
