import assert from "node:assert/strict";
import test from "node:test";

interface ValidationResult {
  valid: boolean;
  errors: Record<string, string>;
}

function validateLoginForm(email: string, password: string): ValidationResult {
  const errors: Record<string, string> = {};
  const trimmedEmail = email.trim();

  if (!trimmedEmail) {
    errors.email = "Vui lòng nhập địa chỉ email.";
  } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(trimmedEmail)) {
    errors.email = "Địa chỉ email không đúng định dạng.";
  }

  if (!password) {
    errors.password = "Vui lòng nhập mật khẩu.";
  }

  return { valid: Object.keys(errors).length === 0, errors };
}

function validateRegisterForm(
  displayName: string,
  email: string,
  password: string,
  confirmPassword: string,
): ValidationResult {
  const errors: Record<string, string> = {};
  const trimmedName = displayName.trim();

  if (!trimmedName) {
    errors.displayName = "Vui lòng nhập tên hiển thị.";
  } else if (trimmedName.length < 2 || trimmedName.length > 100) {
    errors.displayName = "Tên hiển thị phải từ 2 đến 100 ký tự.";
  } else if (/[<>]/.test(trimmedName)) {
    errors.displayName = "Tên hiển thị không được chứa thẻ HTML.";
  }

  const trimmedEmail = email.trim();
  if (!trimmedEmail) {
    errors.email = "Vui lòng nhập địa chỉ email.";
  } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(trimmedEmail)) {
    errors.email = "Địa chỉ email không đúng định dạng.";
  }

  if (!password) {
    errors.password = "Vui lòng nhập mật khẩu.";
  } else if (password.length < 8) {
    errors.password = "Mật khẩu phải có tối thiểu 8 ký tự.";
  } else if (
    !/[A-Z]/.test(password) ||
    !/[a-z]/.test(password) ||
    !/[0-9]/.test(password) ||
    !/[^a-zA-Z0-9]/.test(password)
  ) {
    errors.password =
      "Mật khẩu phải chứa ít nhất 1 chữ hoa, 1 chữ thường, 1 chữ số và 1 ký tự đặc biệt.";
  }

  if (!confirmPassword) {
    errors.confirmPassword = "Vui lòng xác nhận lại mật khẩu.";
  } else if (password !== confirmPassword) {
    errors.confirmPassword = "Mật khẩu xác nhận không khớp.";
  }

  return { valid: Object.keys(errors).length === 0, errors };
}

function validateProfileForm(displayName: string, avatarUrl?: string): ValidationResult {
  const errors: Record<string, string> = {};
  const trimmed = displayName.trim();

  if (!trimmed) {
    errors.displayName = "Vui lòng nhập tên hiển thị.";
  } else if (trimmed.length < 2 || trimmed.length > 100) {
    errors.displayName = "Tên hiển thị phải từ 2 đến 100 ký tự.";
  } else if (/[<>]/.test(trimmed)) {
    errors.displayName = "Tên hiển thị không được chứa thẻ HTML.";
  }

  if (avatarUrl && avatarUrl.trim()) {
    try {
      const parsed = new URL(avatarUrl.trim());
      if (!["http:", "https:"].includes(parsed.protocol)) {
        errors.avatarUrl = "Đường dẫn ảnh đại diện phải bắt đầu bằng http:// hoặc https://";
      }
    } catch {
      errors.avatarUrl = "Địa chỉ URL ảnh đại diện không hợp lệ.";
    }
  }

  return { valid: Object.keys(errors).length === 0, errors };
}

test("LoginForm validation rejects empty email and password", () => {
  const result = validateLoginForm("", "");
  assert.strictEqual(result.valid, false);
  assert.strictEqual(result.errors.email, "Vui lòng nhập địa chỉ email.");
  assert.strictEqual(result.errors.password, "Vui lòng nhập mật khẩu.");
});

test("LoginForm validation rejects invalid email format", () => {
  const result = validateLoginForm("not-an-email", "secret");
  assert.strictEqual(result.valid, false);
  assert.strictEqual(result.errors.email, "Địa chỉ email không đúng định dạng.");
});

test("LoginForm validation passes with valid email and password", () => {
  const result = validateLoginForm("chef@culinary.vn", "Secret123!");
  assert.strictEqual(result.valid, true);
  assert.deepStrictEqual(result.errors, {});
});

test("RegisterForm validation enforces password complexity requirements", () => {
  // Missing uppercase
  let res = validateRegisterForm("User", "u@example.com", "pass123!", "pass123!");
  assert.strictEqual(res.valid, false);
  assert.ok(res.errors.password?.includes("chữ hoa"));

  // Missing special char
  res = validateRegisterForm("User", "u@example.com", "Pass1234", "Pass1234");
  assert.strictEqual(res.valid, false);
  assert.ok(res.errors.password?.includes("ký tự đặc biệt"));

  // Under 8 characters
  res = validateRegisterForm("User", "u@example.com", "P1!a", "P1!a");
  assert.strictEqual(res.valid, false);
  assert.ok(res.errors.password?.includes("tối thiểu 8 ký tự"));
});

test("RegisterForm validation rejects password mismatch", () => {
  const res = validateRegisterForm("Chef", "c@example.com", "Secret123!", "Different123!");
  assert.strictEqual(res.valid, false);
  assert.strictEqual(res.errors.confirmPassword, "Mật khẩu xác nhận không khớp.");
});

test("RegisterForm validation rejects HTML in display name", () => {
  const res = validateRegisterForm("<script>alert(1)</script>", "c@example.com", "Secret123!", "Secret123!");
  assert.strictEqual(res.valid, false);
  assert.strictEqual(res.errors.displayName, "Tên hiển thị không được chứa thẻ HTML.");
});

test("ProfileForm validation verifies display name and avatar URL scheme", () => {
  const invalidUrl = validateProfileForm("Hoài Bơ", "javascript:alert(1)");
  assert.strictEqual(invalidUrl.valid, false);
  assert.ok(invalidUrl.errors.avatarUrl?.includes("http"));

  const valid = validateProfileForm("Phùng Hoài Bơ", "https://culinary.vn/avatar.png");
  assert.strictEqual(valid.valid, true);
});

