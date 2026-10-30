import { expect, test } from "@playwright/test";
import { loginThroughUi, registerAuthor } from "./helpers";

test("logging out from the profile page and signing in again lands on the home page", async ({ page, request }) => {
  const author = await registerAuthor(request);

  await page.goto("/login");
  await loginThroughUi(page, author);
  await expect(page).toHaveURL("/");

  await page.goto("/profile");
  await page.getByRole("button", { name: "Đăng xuất" }).click();
  await expect(page).toHaveURL("/");
  await expect(page).not.toHaveURL(/returnUrl/);

  await page.goto("/login");
  await loginThroughUi(page, author);
  await expect(page).toHaveURL("/");
});
