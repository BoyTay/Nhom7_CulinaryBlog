import { expect, test } from "@playwright/test";
import { createDraftRecipe, deleteRecipe, loginThroughUi, registerAuthor } from "./helpers";

test("an unauthenticated visitor is sent to login and returns to the dashboard afterwards", async ({ page, request }) => {
  const author = await registerAuthor(request);

  await page.goto("/dashboard/recipes");
  await expect(page).toHaveURL(/\/login\?returnUrl=%2Fdashboard%2Frecipes/);

  await loginThroughUi(page, author);
  await expect(page).toHaveURL(/\/dashboard\/recipes$/);
  await expect(page.getByRole("heading", { name: "Công thức của tôi" })).toBeVisible();
});

test("a new author sees an empty dashboard, then publishes, archives and deletes a recipe", async ({ page, request }) => {
  const author = await registerAuthor(request);

  await page.goto("/login");
  await loginThroughUi(page, author);
  await page.goto("/dashboard/recipes");
  await expect(page.getByText("Bạn chưa có công thức nào")).toBeVisible();
  await expect(page.getByRole("link", { name: "Viết công thức đầu tiên" })).toBeVisible();

  const recipe = await createDraftRecipe(request, author, { complete: true, label: "Pho bo E2E" });
  await page.goto("/dashboard/recipes");
  const row = page.getByRole("row", { name: new RegExp(recipe.title) });
  await expect(row.getByText("Bản nháp")).toBeVisible();

  // Publish: success toast, status badge changes, the recipe becomes public.
  await row.getByRole("button", { name: "Xuất bản", exact: true }).click();
  await expect(page.locator(".toast-success", { hasText: "Đã xuất bản công thức" })).toBeVisible();
  await expect(row.getByText("Đã xuất bản")).toBeVisible();

  await page.goto("/recipes");
  await expect(page.getByRole("link", { name: new RegExp(recipe.title) })).toBeVisible();
  await page.goto(`/recipes/${recipe.slug}`);
  await expect(page.getByRole("heading", { level: 1, name: recipe.title })).toBeVisible();
  await expect(page.getByText("Ninh xương trong hai giờ.")).toBeVisible();
  await expect(page.getByText("Bánh phở")).toBeVisible();

  // Archive and delete both ask for confirmation first.
  await page.goto("/dashboard/recipes");
  await row.getByRole("button", { name: "Hủy xuất bản", exact: true }).click();
  await expect(row.getByText("Bản nháp")).toBeVisible();
  await row.getByRole("button", { name: "Xóa", exact: true }).click();
  const dialog = page.getByRole("dialog");
  await expect(dialog).toBeVisible();
  await dialog.getByRole("button", { name: "Hủy", exact: true }).click();
  await expect(row).toBeVisible();

  await row.getByRole("button", { name: "Xóa", exact: true }).click();
  await page.getByRole("dialog").getByRole("button", { name: "Xóa", exact: true }).click();
  await expect(page.locator(".toast-success", { hasText: "Đã xóa công thức" })).toBeVisible();
  await expect(page.getByText("Bạn chưa có công thức nào")).toBeVisible();
});

test("publishing an incomplete recipe explains what is missing", async ({ page, request }) => {
  const author = await registerAuthor(request);
  const recipe = await createDraftRecipe(request, author, { complete: false, label: "Thieu buoc E2E" });

  await page.goto("/login?returnUrl=%2Fdashboard%2Frecipes");
  await loginThroughUi(page, author);
  const row = page.getByRole("row", { name: new RegExp(recipe.title) });
  await row.getByRole("button", { name: "Xuất bản", exact: true }).click();

  await expect(page.locator(".toast-error", { hasText: "nguyên liệu" })).toBeVisible();
  await expect(row.getByText("Bản nháp")).toBeVisible();
  await deleteRecipe(request, author, recipe.id);
});

test("a guest can browse from the home page through a category to a recipe", async ({ page, request }) => {
  const author = await registerAuthor(request);
  const recipe = await createDraftRecipe(request, author, { complete: true, label: "Khach xem E2E" });
  const publish = await request.get(`/api/v1/recipes/${recipe.id}`, { headers: { Authorization: `Bearer ${author.token}` } });
  const response = await request.patch(`/api/v1/recipes/${recipe.id}/publish`, {
    headers: { Authorization: `Bearer ${author.token}`, "If-Match": publish.headers()["etag"] },
  });
  expect(response.status(), "publish").toBe(200);

  await page.goto("/");
  await expect(page.getByRole("link", { name: new RegExp(recipe.title) })).toBeVisible();

  await page.getByRole("navigation", { name: "Chính" }).getByRole("link", { name: "Danh mục" }).click();
  await expect(page).toHaveURL(/\/categories$/);
  await page.getByRole("link", { name: /Món chính/ }).first().click();
  await expect(page).toHaveURL(/\/categories\/mon-chinh/);
  await page.getByRole("link", { name: new RegExp(recipe.title) }).click();
  await expect(page).toHaveURL(new RegExp(`/recipes/${recipe.slug}$`));
  await expect(page.getByRole("heading", { level: 1, name: recipe.title })).toBeVisible();

  // The legacy id URL redirects to the canonical slug URL.
  await page.goto(`/recipes/${recipe.id}`);
  await expect(page).toHaveURL(new RegExp(`/recipes/${recipe.slug}$`));

  await deleteRecipe(request, author, recipe.id);
});

test("an author cannot open the admin category page and unknown pages return 404", async ({ page, request }) => {
  const author = await registerAuthor(request);

  await page.goto("/login");
  await loginThroughUi(page, author);
  await page.goto("/dashboard/categories");
  await expect(page.getByText("Quyền truy cập bị từ chối")).toBeVisible();

  const missingRecipe = await page.goto("/recipes/khong-ton-tai-e2e");
  expect(missingRecipe?.status()).toBe(404);
  const missingCategory = await page.goto("/categories/khong-ton-tai-e2e");
  expect(missingCategory?.status()).toBe(404);
});

test("the old admin URL leads guests to login for the new dashboard page", async ({ page }) => {
  await page.goto("/admin/categories");
  await expect(page).toHaveURL(/\/login\?returnUrl=%2Fdashboard%2Fcategories/);
});
