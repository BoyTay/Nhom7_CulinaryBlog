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
  const row = page.getByRole("listitem").filter({ hasText: recipe.title });
  await expect(row.getByText("Bản nháp")).toBeVisible();

  // Publish: success toast, status badge changes, the recipe becomes public.
  await row.getByRole("button", { name: "Xuất bản", exact: true }).click();
  await expect(page.locator(".toast-success", { hasText: "Đã xuất bản công thức" })).toBeVisible();
  await expect(row.getByText("Đã xuất bản")).toBeVisible();

  await page.goto("/recipes");
  await expect(page.getByRole("link", { name: new RegExp(recipe.title) })).toBeVisible();
  await page.goto(`/recipes/${recipe.slug}`);
  await expect(page.getByRole("heading", { level: 1, name: recipe.title })).toBeVisible();
  await expect(page.locator(".recipe-detail-steps").getByText("Ninh xương trong hai giờ.")).toBeVisible();
  await expect(page.getByText("Bánh phở")).toBeVisible();

  // Archive and delete both ask for confirmation first.
  await page.goto("/dashboard/recipes");
  await row.getByRole("button", { name: "Hủy xuất bản", exact: true }).click();
  await expect(row.getByText("Bản nháp")).toBeVisible();
  await row.getByRole("button", { name: `Xóa ${recipe.title}`, exact: true }).click();
  const dialog = page.getByRole("dialog");
  await expect(dialog).toBeVisible();
  await dialog.getByRole("button", { name: "Giữ nguyên", exact: true }).click();
  await expect(row).toBeVisible();

  await row.getByRole("button", { name: `Xóa ${recipe.title}`, exact: true }).click();
  await page.getByRole("dialog").getByRole("button", { name: "Xóa", exact: true }).click();
  await expect(page.locator(".toast-success", { hasText: "Đã xóa công thức" })).toBeVisible();
  await expect(page.getByText("Bạn chưa có công thức nào")).toBeVisible();
});

test("publishing an incomplete recipe explains what is missing", async ({ page, request }) => {
  const author = await registerAuthor(request);
  const recipe = await createDraftRecipe(request, author, { complete: false, label: "Thieu buoc E2E" });

  await page.goto("/login?returnUrl=%2Fdashboard%2Frecipes");
  await loginThroughUi(page, author);
  const row = page.getByRole("listitem").filter({ hasText: recipe.title });
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

test("dashboard tabs filter by status and an archived recipe can be restored to draft", async ({ page, request }) => {
  const author = await registerAuthor(request);
  const archived = await createDraftRecipe(request, author, { complete: true, label: "Luu tru E2E" });
  const draft = await createDraftRecipe(request, author, { complete: false, label: "Nhap E2E" });
  const etagOf = async (id: string) =>
    (await request.get(`/api/v1/recipes/${id}`, { headers: { Authorization: `Bearer ${author.token}` } })).headers()["etag"];
  const authHeaders = async (id: string) => ({ Authorization: `Bearer ${author.token}`, "If-Match": await etagOf(id) });
  expect((await request.patch(`/api/v1/recipes/${archived.id}/archive`, { headers: await authHeaders(archived.id) })).status()).toBe(200);

  await page.goto("/login?returnUrl=%2Fdashboard%2Frecipes");
  await loginThroughUi(page, author);
  await expect(page.getByRole("main").getByRole("link", { name: "Viết công thức mới" })).toBeVisible();
  await expect(page.getByRole("complementary").getByText("Tác giả")).toBeVisible();

  await page.getByRole("tab", { name: "Lưu trữ" }).click();
  await expect(page).toHaveURL(/status=archived/);
  await expect(page.getByRole("listitem").filter({ hasText: archived.title })).toBeVisible();
  await expect(page.getByRole("listitem").filter({ hasText: draft.title })).toHaveCount(0);

  await page.getByRole("button", { name: "Khôi phục", exact: true }).click();
  await expect(page.locator(".toast-success", { hasText: "Đã khôi phục" })).toBeVisible();
  await expect(page.getByText("Không có công thức ở mục này")).toBeVisible();

  await page.getByRole("tab", { name: "Bản nháp" }).click();
  await expect(page.getByRole("listitem").filter({ hasText: archived.title })).toBeVisible();
  await expect(page.getByRole("listitem").filter({ hasText: draft.title })).toBeVisible();
});

test("an uploaded photo is shown on the recipe card instead of the placeholder", async ({ page, request }) => {
  const { createPng } = await import("./helpers");
  const author = await registerAuthor(request);
  const recipe = await createDraftRecipe(request, author, { complete: true, label: "Co anh E2E" });
  const etag = async () =>
    (await request.get(`/api/v1/recipes/${recipe.id}`, { headers: { Authorization: `Bearer ${author.token}` } })).headers()["etag"];

  const upload = await request.post(`/api/v1/recipes/${recipe.id}/images`, {
    headers: { Authorization: `Bearer ${author.token}`, "If-Match": await etag() },
    multipart: { file: { name: "anh.png", mimeType: "image/png", buffer: createPng(640, 480) }, altText: "Ảnh thử", isPrimary: "true" },
  });
  expect(upload.status(), "upload").toBe(201);
  const published = await request.patch(`/api/v1/recipes/${recipe.id}/publish`, {
    headers: { Authorization: `Bearer ${author.token}`, "If-Match": await etag() },
  });
  expect(published.status(), "publish").toBe(200);

  await page.goto("/recipes");
  const image = page.getByRole("link", { name: new RegExp(recipe.title) }).locator("img");
  await expect(image).toHaveAttribute("src", /localhost:9000\/culinary-blog\/recipes\//);
  await expect.poll(() => image.evaluate((el: HTMLImageElement) => el.complete && el.naturalWidth > 0)).toBe(true);

  await deleteRecipe(request, author, recipe.id);
});
