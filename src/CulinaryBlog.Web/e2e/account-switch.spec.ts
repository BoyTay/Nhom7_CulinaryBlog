import { expect, test } from "@playwright/test";
import { createDraftRecipe, loginThroughUi, registerAuthor } from "./helpers";

// Regression: the client-side query cache outlived the session, so after signing out and signing in as someone
// else the dashboard kept showing the previous user's recipes until a full page reload. Every step below is a
// client-side navigation on purpose: a reload would empty the cache and hide the bug.
test("signing in as another user after signing out never shows the previous user's recipes", async ({ page, request }) => {
  const first = await registerAuthor(request);
  const second = await registerAuthor(request);
  const recipe = await createDraftRecipe(request, first, { complete: false, label: "Cache switch E2E" });

  await page.goto("/login?returnUrl=%2Fdashboard%2Frecipes");
  await loginThroughUi(page, first);
  await expect(page.getByRole("listitem").filter({ hasText: recipe.title })).toBeVisible();

  const accountMenu = page.locator("details.hd-user");
  await accountMenu.locator("summary").click();
  await accountMenu.getByRole("button", { name: "Đăng xuất" }).click();
  await expect(page).toHaveURL("/");

  await page.getByRole("banner").getByRole("link", { name: "Đăng nhập", exact: true }).click();
  await loginThroughUi(page, second);

  await accountMenu.locator("summary").click();
  await accountMenu.getByRole("link", { name: "Công thức của tôi" }).click();
  await expect(page).toHaveURL(/\/dashboard\/recipes$/);

  await expect(page.getByText("Bạn chưa có công thức nào")).toBeVisible();
  await expect(page.getByText(recipe.title)).toHaveCount(0);
});
