import { randomBytes } from "node:crypto";
import { expect, test } from "@playwright/test";
import { createPng } from "./helpers";

// Needs the development Admin password; skipped when it is not provided.
const adminEmail = process.env.ADMIN_EMAIL ?? "admin@culinaryblog.local";
const adminPassword = process.env.ADMIN_PASSWORD;

test("an admin uploads a category photo from the dashboard and it appears on /categories", async ({ page, request }) => {
  test.skip(!adminPassword, "ADMIN_PASSWORD is not set");

  const login = await request.post("/api/v1/auth/login", { data: { email: adminEmail, password: adminPassword } });
  expect(login.status(), "admin login").toBe(200);
  const token = ((await login.json()) as { accessToken: string }).accessToken;
  const auth = { Authorization: `Bearer ${token}` };

  const name = `Anh E2E ${randomBytes(3).toString("hex")}`;
  const created = await request.post("/api/v1/categories", { headers: auth, data: { name, description: "Danh mục thử ảnh", imageUrl: null, orderIndex: 99 } });
  expect(created.status(), "create category").toBe(201);
  const category = (await created.json()) as { id: string };

  try {
    await page.goto("/login?returnUrl=%2Fdashboard%2Fcategories");
    await page.locator("#em").fill(adminEmail);
    await page.locator("#pw").fill(adminPassword ?? "");
    await page.locator("button[type=submit]").click();
    await page.waitForURL((url) => url.pathname.startsWith("/dashboard/categories"));

    // No manual token field any more: the signed-in session is used.
    await expect(page.getByLabel("Admin access token")).toHaveCount(0);

    await page.getByRole("button", { name: `Sửa ${name}` }).click();
    await page.locator('input[type="file"]').setInputFiles({ name: "danh-muc.png", mimeType: "image/png", buffer: createPng(640, 480) });
    await expect(page.getByText("Đã tải ảnh danh mục lên.")).toBeVisible();

    await page.goto("/categories");
    const image = page.getByRole("img", { name: `Ảnh danh mục ${name}` });
    await expect(image).toHaveAttribute("src", /localhost:9000\/culinary-blog\/categories\//);
    await expect.poll(() => image.evaluate((el: HTMLImageElement) => el.complete && el.naturalWidth > 0)).toBe(true);
  } finally {
    await request.delete(`/api/v1/categories/${category.id}`, { headers: auth });
  }
});
