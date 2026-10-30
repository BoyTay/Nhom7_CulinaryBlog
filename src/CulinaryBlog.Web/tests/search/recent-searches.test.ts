import assert from "node:assert/strict";
import test from "node:test";
import { addRecentSearch } from "../../lib/search/recent-searches.ts";

test("the newest search comes first and repeats move to the top", () => {
  assert.deepEqual(addRecentSearch(["bánh xèo", "phở"], "Phở"), ["Phở", "bánh xèo"]);
});

test("only the three latest searches are kept", () => {
  assert.deepEqual(addRecentSearch(["a", "b", "c"], "d"), ["d", "a", "b"]);
});
