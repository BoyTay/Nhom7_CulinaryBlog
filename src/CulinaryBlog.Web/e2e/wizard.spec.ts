import { randomBytes } from "node:crypto";
import { expect, test } from "@playwright/test";
import { deleteRecipe, loginThroughUi, registerAuthor } from "./helpers";

test("creating a recipe in the wizard reopens on the photo step, not step 1", async ({ page, request }) => {
  const author = await registerAuthor(request);
  const title = `Mon thu wizard ${randomBytes(3).toString("hex")}`;

  await page.goto("/login?returnUrl=%2Fdashboard%2Frecipes%2Fnew");
  await loginThroughUi(page, author);

  await page.getByLabel(/Tiêu đề/).fill(title);
  await page.getByLabel(/Mô tả/).fill("Công thức thử nghiệm để kiểm tra luồng tạo bản nháp.");
  await page.getByLabel(/Danh mục/).selectOption({ index: 1 });
  for (let i = 0; i < 3; i += 1) await page.getByRole("button", { name: "Tiếp tục" }).click();

  await expect(page.getByRole("heading", { name: "Ảnh và dinh dưỡng" })).toBeVisible();
  // Reaching the last step must not submit by itself: nothing is created until this button is pressed.
  await expect(page).toHaveURL(/\/dashboard\/recipes\/new$/);
  await expect(page.getByRole("button", { name: "Tạo bản nháp" })).toBeEnabled();
  await page.getByRole("button", { name: "Tạo bản nháp" }).click();

  // The editor reopens on the same step so photos can be added immediately.
  await expect(page).toHaveURL(/\/dashboard\/recipes\/[0-9a-f-]{36}\/edit\?created=1$/);
  await expect(page.getByRole("heading", { name: "Ảnh và dinh dưỡng" })).toBeVisible();
  await expect(page.getByText("Đã tạo bản nháp. Bạn có thể thêm ảnh ngay bây giờ.")).toBeVisible();
  await expect(page.locator('input[type="file"]')).toBeVisible();

  const id = page.url().match(/recipes\/([0-9a-f-]{36})\/edit/)?.[1] ?? "";
  await deleteRecipe(request, author, id);
});
