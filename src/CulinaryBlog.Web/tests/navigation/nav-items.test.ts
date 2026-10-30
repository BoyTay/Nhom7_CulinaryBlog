import assert from "node:assert/strict";
import test from "node:test";
import { buildNavItems, showsSiteChrome } from "../../lib/navigation/routes.ts";

const labels = (roles: string[] | null) =>
  buildNavItems(roles ? { roles } : null).map((item) => item.label);

test("guests see the public links only", () => {
  assert.deepEqual(labels(null), ["Công thức", "Danh mục", "Tìm kiếm"]);
});

test("authors additionally see the dashboard", () => {
  assert.deepEqual(labels(["Author"]), ["Công thức", "Danh mục", "Tìm kiếm", "Bảng điều khiển"]);
});

test("admins get a single dashboard link (category management lives in the dashboard sidebar) and role matching ignores case", () => {
  assert.deepEqual(labels(["admin"]), ["Công thức", "Danh mục", "Tìm kiếm", "Bảng điều khiển"]);
});

test("unknown roles get no extra links", () => {
  assert.deepEqual(labels(["Moderator"]), ["Công thức", "Danh mục", "Tìm kiếm"]);
});

test("auth screens and the recipe editor and the dashboard render without the shared chrome", () => {
  for (const path of ["/login", "/register", "/forgot-password", "/reset-password", "/dashboard", "/dashboard/recipes", "/dashboard/categories", "/dashboard/recipes/new", "/dashboard/recipes/abc/edit"]) {
    assert.equal(showsSiteChrome(path), false, path);
  }
  for (const path of ["/", "/recipes", "/recipes/pho-bo", "/profile"]) {
    assert.equal(showsSiteChrome(path), true, path);
  }
});
