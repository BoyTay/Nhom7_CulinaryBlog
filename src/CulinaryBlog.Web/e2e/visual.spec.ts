import { mkdirSync } from "node:fs";
import { join } from "node:path";
import { expect, test, type Page } from "@playwright/test";
import { createDraftRecipe, deleteRecipe, loginThroughUi, registerAuthor, type TestAuthor } from "./helpers";

// Structural parity checks plus screenshots for side-by-side review against docs/design/project/*.dc.html.
// Screenshots go to e2e/__screens__/ (git-ignored).
const screens = join(process.cwd(), "e2e", "__screens__");
mkdirSync(screens, { recursive: true });

const viewports = [
  { name: "desktop", size: { width: 1440, height: 900 } },
  { name: "mobile", size: { width: 390, height: 844 } },
];

async function assertPageShape(page: Page, name: string, viewport: string) {
  await expect(page.locator("h1")).toHaveCount(1);
  await expect(page.locator("main")).toHaveCount(1);
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
  expect(overflow, `${name} scrolls horizontally on ${viewport}`).toBeLessThanOrEqual(0);
  await page.screenshot({ path: join(screens, `${viewport}-${name}.png`), fullPage: true });
}

for (const viewport of viewports) {
  test.describe(`${viewport.name} public pages`, () => {
    test.use({ viewport: viewport.size });

    test("every public page has one h1, a main landmark and no horizontal scroll", async ({ page, request }) => {
      const author = await registerAuthor(request);
      const recipe = await createDraftRecipe(request, author, { complete: true, label: "Hinh thuc E2E" });
      const detail = await request.get(`/api/v1/recipes/${recipe.id}`, { headers: { Authorization: `Bearer ${author.token}` } });
      const published = await request.patch(`/api/v1/recipes/${recipe.id}/publish`, {
        headers: { Authorization: `Bearer ${author.token}`, "If-Match": detail.headers()["etag"] },
      });
      expect(published.status()).toBe(200);

      const pages: Array<[string, string]> = [
        ["home", "/"],
        ["recipes", "/recipes"],
        ["detail", `/recipes/${recipe.slug}`],
        ["categories", "/categories"],
        ["search", "/search?q=hinh"],
        ["login", "/login"],
        ["register", "/register"],
      ];
      for (const [name, path] of pages) {
        await page.goto(path, { waitUntil: "networkidle" });
        await assertPageShape(page, name, viewport.name);
      }

      await deleteRecipe(request, author, recipe.id);
    });
  });

  test.describe(`${viewport.name} signed-in pages`, () => {
    test.use({ viewport: viewport.size });

    test("dashboard, editor and profile keep the same structure", async ({ page, request }) => {
      const author: TestAuthor = await registerAuthor(request);
      await page.goto("/login?returnUrl=%2Fdashboard");
      await loginThroughUi(page, author);

      for (const [name, path] of [
        ["dashboard", "/dashboard"],
        ["dashboard-recipes", "/dashboard/recipes"],
        ["editor", "/dashboard/recipes/new"],
        ["profile", "/profile"],
      ] as const) {
        await page.goto(path, { waitUntil: "networkidle" });
        await page.waitForTimeout(400);
        await assertPageShape(page, name, viewport.name);
      }
    });
  });
}

test.describe("mobile touch targets", () => {
  test.use({ viewport: { width: 390, height: 844 } });

  test("header controls, buttons and chips are at least 44px tall and the category chips scroll on their own", async ({ page }) => {
    for (const path of ["/", "/recipes", "/categories", "/login"]) {
      await page.goto(path, { waitUntil: "networkidle" });
      const small = await page.evaluate(() => {
        const selectors = [".site-header-icon", ".btn", ".chip", ".site-header-brand"];
        return selectors.flatMap((selector) =>
          [...document.querySelectorAll<HTMLElement>(selector)]
            .filter((el) => el.offsetParent !== null)
            .map((el) => ({ selector, height: Math.round(el.getBoundingClientRect().height), text: (el.textContent ?? "").trim().slice(0, 24) }))
            .filter((item) => item.height < 44 && item.selector !== ".site-header-brand"),
        );
      });
      expect(small, `${path} has small touch targets`).toEqual([]);
    }

    await page.goto("/", { waitUntil: "networkidle" });
    const chips = page.locator(".home-categories ul");
    if (await chips.count()) {
      const scrollsInside = await chips.evaluate((el) => getComputedStyle(el).flexWrap === "nowrap" && getComputedStyle(el).overflowX === "auto");
      expect(scrollsInside).toBe(true);
    }
  });
});
