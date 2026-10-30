import assert from "node:assert/strict";
import test from "node:test";
import { assignPersistedIds } from "../../lib/recipes/reorder.ts";

const saved = [{ id: "a", name: "Muối" }, { id: "b", name: "Đường" }, { id: "c", name: "Tiêu" }];

test("moved rows keep the saved id order so only their content changes", () => {
  const moved = [saved[2], saved[0], saved[1]];
  assert.deepEqual(assignPersistedIds(moved, saved), [
    { id: "a", name: "Tiêu" },
    { id: "b", name: "Muối" },
    { id: "c", name: "Đường" },
  ]);
});

test("an unchanged list keeps every row as it was", () => {
  assert.deepEqual(assignPersistedIds(saved, saved), saved);
});

test("a new row moved between saved rows still ends up in that position", () => {
  const rows: { id?: string; name: string }[] = [saved[0], { name: "Mới" }, saved[1]];
  assert.deepEqual(assignPersistedIds(rows, saved), [
    { id: "a", name: "Muối" },
    { id: "b", name: "Mới" },
    { id: undefined, name: "Đường" },
  ]);
});

test("a removed row gives up its id", () => {
  const rows = [saved[2], saved[0]];
  assert.deepEqual(assignPersistedIds(rows, saved), [
    { id: "a", name: "Tiêu" },
    { id: "c", name: "Muối" },
  ]);
});
