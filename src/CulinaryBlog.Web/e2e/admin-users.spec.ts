import { expect, test, type Page } from "@playwright/test";
import { createDraftRecipe, loginThroughUi, registerAuthor } from "./helpers";

// Needs the development Admin password; skipped when it is not provided.
const adminEmail = process.env.ADMIN_EMAIL ?? "admin@culinaryblog.local";
const adminPassword = process.env.ADMIN_PASSWORD;

async function openUsersAsAdmin(page: Page): Promise<void> {
  await page.goto("/login?returnUrl=%2Fdashboard%2Fusers");
  await page.locator("#em").fill(adminEmail);
  await page.locator("#pw").fill(adminPassword ?? "");
  await page.locator("button[type=submit]").click();
  await page.waitForURL((url) => url.pathname === "/dashboard/users");
}

test("an admin finds an author, changes the role, locks and unlocks the account, and cannot delete it", async ({ page, request }) => {
  test.skip(!adminPassword, "ADMIN_PASSWORD is not set");

  const author = await registerAuthor(request);
  await createDraftRecipe(request, author, { complete: false, label: "Tai khoan quan ly" });
  const displayName = `E2E ${author.email.slice(4, 14)}`;

  await openUsersAsAdmin(page);
  await expect(page.getByRole("heading", { name: "Quản lý tài khoản", level: 1 })).toBeVisible();
  await expect(page.getByLabel("Tổng quan tài khoản").getByText("Tham gia 30 ngày qua")).toBeVisible();

  await page.getByLabel("Tìm tài khoản").fill(author.email);
  const row = page.getByRole("listitem").filter({ hasText: author.email });
  await expect(row).toHaveCount(1);
  await expect(row.getByText("Tác giả")).toBeVisible();
  await expect(row.getByText("1 công thức")).toBeVisible();

  await page.getByRole("button", { name: `Đổi vai trò của ${displayName}` }).click();
  await page.getByRole("dialog").getByText("Admin", { exact: true }).click();
  await page.getByRole("button", { name: "Lưu vai trò" }).click();
  await expect(page.getByText(`Đã đổi vai trò của ${displayName} thành Admin`)).toBeVisible();
  await expect(row.getByText("Admin", { exact: true })).toBeVisible();

  await page.getByRole("button", { name: `Khóa ${displayName}` }).click();
  await page.getByRole("dialog").getByText("30 ngày", { exact: true }).click();
  await page.getByRole("button", { name: "Khóa tài khoản", exact: true }).click();
  await expect(page.getByText(`Đã khóa tài khoản của ${displayName} trong 30 ngày`)).toBeVisible();
  await expect(row.getByText("Bị khóa")).toBeVisible();
  await expect(row.getByText(/Khóa đến \d{2}\/\d{2}\/\d{4} \(30 ngày\)/)).toBeVisible();

  // The token issued at registration stops working at once, not when it expires.
  const stopped = await request.get("/api/v1/auth/me", { headers: { Authorization: `Bearer ${author.token}` } });
  expect(stopped.status(), "existing token of a locked account").toBe(401);
  expect(((await stopped.json()) as { type: string }).type).toBe("ACCOUNT_LOCKED");

  const blocked = await request.post("/api/v1/auth/login", { data: { email: author.email, password: author.password } });
  expect(blocked.status(), "locked login").toBe(401);
  expect(((await blocked.json()) as { type: string }).type).toBe("ACCOUNT_LOCKED");

  await page.getByRole("button", { name: `Mở khóa ${displayName}` }).click();
  await page.getByRole("button", { name: "Mở khóa", exact: true }).click();
  await expect(row.getByText("Hoạt động")).toBeVisible();

  const restored = await request.post("/api/v1/auth/login", { data: { email: author.email, password: author.password } });
  expect(restored.status(), "unlocked login").toBe(200);

  await expect(row.getByRole("button", { name: /Xóa/ })).toHaveCount(0);
});

test("an author who is locked while working is told so on the next action", async ({ page, request }) => {
  test.skip(!adminPassword, "ADMIN_PASSWORD is not set");

  const author = await registerAuthor(request);
  const recipe = await createDraftRecipe(request, author, { complete: false, label: "Dang lam viec" });
  await page.goto("/login?returnUrl=%2Fdashboard%2Frecipes");
  await loginThroughUi(page, author);
  const row = page.getByRole("listitem").filter({ hasText: recipe.title });
  await expect(row).toBeVisible();

  const login = await request.post("/api/v1/auth/login", { data: { email: adminEmail, password: adminPassword } });
  const auth = { Authorization: `Bearer ${((await login.json()) as { accessToken: string }).accessToken}` };
  const found = await request.get(`/api/v1/users?q=${encodeURIComponent(author.email)}`, { headers: auth });
  const { users } = (await found.json()) as { users: { items: { id: string }[] } };
  const locked = await request.put(`/api/v1/users/${users.items[0].id}/lock`, { headers: auth, data: { locked: true } });
  expect(locked.status(), "lock").toBe(204);

  await row.getByRole("button", { name: "Xuất bản", exact: true }).click();

  await expect(page.getByRole("heading", { name: "Tài khoản của bạn đang bị khóa" })).toBeVisible();
  await expect(page.getByRole("link", { name: "Đăng nhập lại" })).toBeVisible();
});

test("the signed-in admin's own row offers no actions", async ({ page }) => {
  test.skip(!adminPassword, "ADMIN_PASSWORD is not set");

  await openUsersAsAdmin(page);
  await page.getByLabel("Tìm tài khoản").fill(adminEmail);
  const row = page.getByRole("listitem").filter({ hasText: adminEmail });
  await expect(row.getByText("(bạn)")).toBeVisible();
  await expect(row.getByText("Không thể tự khóa tài khoản đang đăng nhập")).toBeVisible();
  await expect(row.getByRole("button")).toHaveCount(0);
});

test("an author cannot open the accounts page", async ({ page, request }) => {
  const author = await registerAuthor(request);

  await page.goto("/login?returnUrl=%2Fdashboard%2Fusers");
  await page.locator("#em").fill(author.email);
  await page.locator("#pw").fill(author.password);
  await page.locator("button[type=submit]").click();
  await page.waitForURL((url) => url.pathname === "/dashboard/users");

  await expect(page.getByRole("heading", { name: "Quản lý tài khoản", level: 1 })).toHaveCount(0);
  await expect(page.getByRole("link", { name: "Tài khoản", exact: true })).toHaveCount(0);
});
