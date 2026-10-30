import { randomBytes } from "node:crypto";
import { expect, test } from "@playwright/test";
import { createDraftRecipe, deleteRecipe, loginThroughUi, registerAuthor } from "./helpers";

test("creating a recipe in the wizard reopens on the photo step, not step 1", async ({ page, request }) => {
  const author = await registerAuthor(request);
  const title = `Mon thu wizard ${randomBytes(3).toString("hex")}`;

  await page.goto("/login?returnUrl=%2Fdashboard%2Frecipes%2Fnew");
  await loginThroughUi(page, author);

  await page.getByLabel(/Tiêu đề/).fill(title);
  await page.getByLabel(/Mô tả/).fill("Công thức thử nghiệm để kiểm tra luồng tạo bản nháp.");
  await page.getByLabel(/Danh mục/).selectOption({ index: 1 });
  for (let i = 0; i < 3; i += 1) await page.getByRole("button", { name: "Tiếp tục" }).click();

  await expect(page.getByRole("heading", { name: "Ảnh & dinh dưỡng" })).toBeVisible();
  // Reaching the last step must not submit by itself: nothing is created until this button is pressed.
  await expect(page).toHaveURL(/\/dashboard\/recipes\/new$/);
  await expect(page.getByRole("button", { name: "Tạo bản nháp" })).toBeEnabled();
  await page.getByRole("button", { name: "Tạo bản nháp" }).click();

  // The editor reopens on the same step so photos can be added immediately.
  await expect(page).toHaveURL(/\/dashboard\/recipes\/[0-9a-f-]{36}\/edit\?created=1$/);
  await expect(page.getByRole("heading", { name: "Ảnh & dinh dưỡng" })).toBeVisible();
  await expect(page.getByText("Đã tạo bản nháp. Bạn có thể thêm ảnh ngay bây giờ.")).toBeVisible();
  await expect(page.locator('input[type="file"]')).toBeVisible();

  const id = page.url().match(/recipes\/([0-9a-f-]{36})\/edit/)?.[1] ?? "";
  await deleteRecipe(request, author, id);
});

test("moving an ingredient and a step keeps the new order after saving", async ({ page, request }) => {
  const author = await registerAuthor(request);
  const recipe = await createDraftRecipe(request, author, { complete: true, label: "Sap xep E2E" });

  try {
    await page.goto(`/login?returnUrl=${encodeURIComponent(`/dashboard/recipes/${recipe.id}/edit`)}`);
    await loginThroughUi(page, author);
    const stepper = page.locator(".recipe-editor-step");

    await stepper.nth(1).click();
    await page.getByRole("button", { name: "+ Thêm nguyên liệu" }).click();
    await page.getByLabel(/^Tên/).last().fill("Muối");
    await page.getByLabel(/^Đơn vị/).last().fill("g");
    await page.getByRole("button", { name: "Chuyển nguyên liệu 2 lên" }).click();

    await stepper.nth(2).click();
    await page.getByRole("button", { name: "+ Thêm bước" }).click();
    await page.locator(".recipe-editor-step-row textarea").last().fill("Sơ chế trước khi nấu.");
    await page.getByRole("button", { name: "Chuyển bước 2 lên" }).click();

    await stepper.nth(3).click();
    await page.getByRole("button", { name: "Lưu thay đổi" }).click();
    await expect(page.getByText("Đã lưu thay đổi.")).toBeVisible();

    const response = await request.get(`/api/v1/recipes/${recipe.id}`, { headers: { Authorization: `Bearer ${author.token}` } });
    const detail = (await response.json()) as {
      ingredients: { name: string; sortOrder: number }[];
      steps: { description: string; stepNumber: number }[];
    };
    const ingredients = [...detail.ingredients].sort((a, b) => a.sortOrder - b.sortOrder).map((row) => row.name);
    const steps = [...detail.steps].sort((a, b) => a.stepNumber - b.stepNumber).map((row) => row.description);
    expect(ingredients).toEqual(["Muối", "Bánh phở"]);
    expect(steps).toEqual(["Sơ chế trước khi nấu.", "Ninh xương trong hai giờ."]);
  } finally {
    await deleteRecipe(request, author, recipe.id);
  }
});
