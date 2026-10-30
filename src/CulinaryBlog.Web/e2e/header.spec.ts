import { expect, test } from "@playwright/test";
import { waitForHydration } from "./helpers";

test("the home page search sends the query to the search page", async ({ page }) => {
  await page.goto("/");
  await page.getByRole("searchbox", { name: "Tìm công thức" }).fill("pho bo");
  await page.keyboard.press("Enter");
  await expect(page).toHaveURL(/\/search\?q=pho(?:\+|%20)bo$/);
});

test("the current section is marked in the main navigation", async ({ page }) => {
  await page.goto("/categories");
  const nav = page.getByRole("navigation", { name: "Chính" });
  await expect(nav.getByRole("link", { name: "Danh mục" })).toHaveAttribute("aria-current", "page");
  await expect(nav.getByRole("link", { name: "Công thức" })).not.toHaveAttribute("aria-current", "page");
});

test.describe("on a phone", () => {
  test.use({ viewport: { width: 390, height: 844 } });

  test("the menu opens, closes with Escape and returns focus to its button", async ({ page }) => {
    await page.goto("/");
    const button = page.getByRole("button", { name: "Mở menu" });
    await expect(page.locator("#mobile-menu")).toBeHidden();

    await button.click();
    await expect(page.locator("#mobile-menu")).toBeVisible();
    await expect(page.getByRole("button", { name: "Đóng menu" })).toHaveAttribute("aria-expanded", "true");
    await expect(page.locator("#mobile-menu").getByRole("link", { name: "Danh mục" })).toBeVisible();

    await page.keyboard.press("Escape");
    await expect(page.locator("#mobile-menu")).toBeHidden();
    await expect(page.getByRole("button", { name: "Mở menu" })).toBeFocused();
  });

  test("there is no horizontal page scroll on the home page", async ({ page }) => {
    await page.goto("/");
    const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
    expect(overflow).toBeLessThanOrEqual(0);
  });
});

test("the recipe list filters by text, difficulty and sort and keeps them in the URL", async ({ page }) => {
  await page.goto("/recipes");
  await waitForHydration(page, "#recipe-search");
  const search = page.getByRole("searchbox", { name: "Tìm kiếm" });
  await search.fill("khong co mon nao ten nay");
  await search.press("Enter");
  await expect(page).toHaveURL(/q=khong(?:\+|%20)co(?:\+|%20)mon/);
  await expect(page.getByText("Chưa có công thức nào khớp bộ lọc")).toBeVisible();

  await page.goto("/recipes?difficulty=Hard&sort=name&maxCook=30");
  await waitForHydration(page, "#recipe-search");
  await expect(page.getByRole("button", { name: "Khó", exact: true })).toHaveAttribute("aria-pressed", "true");
  await expect(page.getByLabel("Sắp xếp")).toHaveValue("name");
  await expect(page.getByRole("button", { name: "≤ 30 phút" })).toHaveAttribute("aria-pressed", "true");

  await page.getByRole("button", { name: "Khó", exact: true }).click();
  await expect(page).not.toHaveURL(/difficulty=/);
  await expect(page).toHaveURL(/sort=name/);
});

test("search results are links and the filters open on demand", async ({ page, request }) => {
  const { registerAuthor, createDraftRecipe, deleteRecipe } = await import("./helpers");
  const author = await registerAuthor(request);
  const recipe = await createDraftRecipe(request, author, { complete: true, label: "Tim kiem E2E" });
  const detail = await request.get(`/api/v1/recipes/${recipe.id}`, { headers: { Authorization: `Bearer ${author.token}` } });
  const published = await request.patch(`/api/v1/recipes/${recipe.id}/publish`, {
    headers: { Authorization: `Bearer ${author.token}`, "If-Match": detail.headers()["etag"] },
  });
  expect(published.status()).toBe(200);

  await page.goto(`/search?q=${encodeURIComponent(recipe.title.split(" ")[0] + " " + recipe.title.split(" ")[1])}`);
  await expect(page.getByRole("heading", { level: 2, name: /Kết quả cho/ })).toBeVisible();
  await expect(page.locator("#search-filters")).toBeHidden();
  await page.getByRole("button", { name: /Bộ lọc/ }).click();
  await expect(page.locator("#search-filters")).toBeVisible();

  const row = page.getByRole("link", { name: new RegExp(recipe.title) });
  await row.click();
  await expect(page).toHaveURL(new RegExp(`/recipes/${recipe.slug}$`));

  await deleteRecipe(request, author, recipe.id);
});

test("search results show the recipe photo", async ({ page, request }) => {
  const { registerAuthor, createDraftRecipe, deleteRecipe, createPng } = await import("./helpers");
  const author = await registerAuthor(request);
  const recipe = await createDraftRecipe(request, author, { complete: true, label: "Anh tim E2E" });
  const etag = async () =>
    (await request.get(`/api/v1/recipes/${recipe.id}`, { headers: { Authorization: `Bearer ${author.token}` } })).headers()["etag"];
  const upload = await request.post(`/api/v1/recipes/${recipe.id}/images`, {
    headers: { Authorization: `Bearer ${author.token}`, "If-Match": await etag() },
    multipart: { file: { name: "anh.png", mimeType: "image/png", buffer: createPng(640, 480) }, altText: "Ảnh thử", isPrimary: "true" },
  });
  expect(upload.status(), "upload").toBe(201);
  expect((await request.patch(`/api/v1/recipes/${recipe.id}/publish`, { headers: { Authorization: `Bearer ${author.token}`, "If-Match": await etag() } })).status()).toBe(200);

  await page.goto(`/search?q=${encodeURIComponent("Anh tim")}`);
  const image = page.getByRole("img", { name: `Ảnh món ${recipe.title}` });
  await expect(image).toHaveAttribute("src", /localhost:9000\/culinary-blog\/recipes\//);
  await expect.poll(() => image.evaluate((el: HTMLImageElement) => el.complete && el.naturalWidth > 0)).toBe(true);

  await deleteRecipe(request, author, recipe.id);
});
