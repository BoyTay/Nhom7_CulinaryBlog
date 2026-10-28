import { expect, test } from "@playwright/test";

test("the header search sends the query to the search page", async ({ page }) => {
  await page.goto("/recipes");
  await page.getByRole("searchbox", { name: "Tìm công thức" }).first().fill("pho bo");
  await page.keyboard.press("Enter");
  await expect(page).toHaveURL(/\/search\?q=pho%20bo$/);
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
  const search = page.getByRole("searchbox", { name: "Tìm kiếm" });
  await search.fill("khong co mon nao ten nay");
  await search.press("Enter");
  await expect(page).toHaveURL(/q=khong(+|%20)co(+|%20)mon/);
  await expect(page.getByText("Không có công thức phù hợp")).toBeVisible();

  await page.goto("/recipes?difficulty=Hard&sort=name&maxCook=30");
  await expect(page.getByRole("button", { name: "Khó", exact: true })).toHaveAttribute("aria-pressed", "true");
  await expect(page.getByLabel("Sắp xếp")).toHaveValue("name");
  await expect(page.getByLabel("Nấu tối đa")).toHaveValue("30");

  await page.getByRole("button", { name: "Tất cả", exact: true }).click();
  await expect(page).not.toHaveURL(/difficulty=/);
  await expect(page).toHaveURL(/sort=name/);
});
