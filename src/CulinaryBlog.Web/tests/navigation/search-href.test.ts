import assert from "node:assert/strict";
import test from "node:test";
import { buildSearchHref } from "../../lib/navigation/search-href.ts";

test("buildSearchHref targets /search with an encoded query", () => {
  assert.equal(buildSearchHref("pho bo"), "/search?q=pho%20bo");
  assert.equal(buildSearchHref("  pho bo  "), "/search?q=pho%20bo");
  assert.equal(buildSearchHref("phở & bò"), "/search?q=ph%E1%BB%9F%20%26%20b%C3%B2");
});

test("buildSearchHref drops queries shorter than two characters", () => {
  assert.equal(buildSearchHref(""), "/search");
  assert.equal(buildSearchHref("a"), "/search");
  assert.equal(buildSearchHref("   "), "/search");
});
