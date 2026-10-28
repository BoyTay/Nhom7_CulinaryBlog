import assert from "node:assert/strict";
import test from "node:test";
import { ApiError } from "../../lib/api/client.ts";
import { describeAuthError, fieldErrorsOf, passwordPolicyError } from "../../lib/auth/auth-errors.ts";

test("passwordPolicyError mirrors the server password rules", () => {
  assert.equal(passwordPolicyError("Aa1!aaaa"), null);
  assert.match(passwordPolicyError("Aa1!") ?? "", /8 ký tự/);
  assert.match(passwordPolicyError("aa1!aaaa") ?? "", /in hoa/);
  assert.match(passwordPolicyError("AA1!AAAA") ?? "", /thường/);
  assert.match(passwordPolicyError("Aaa!aaaa") ?? "", /chữ số/);
  assert.match(passwordPolicyError("Aa1aaaaa") ?? "", /đặc biệt/);
});

test("describeAuthError localizes known problem codes and rate limits", () => {
  const invalidLink = new ApiError(409, { status: 409, type: "INVALID_RESET_TOKEN" });
  assert.match(describeAuthError(invalidLink, "fallback"), /không hợp lệ hoặc đã hết hạn/);

  const tooLarge = new ApiError(400, { status: 400, type: "FILE_SIZE_EXCEEDED" });
  assert.match(describeAuthError(tooLarge, "fallback"), /5 MB/);

  const limited = new ApiError(429, { status: 429, type: "RATE_LIMITED" });
  assert.match(describeAuthError(limited, "fallback"), /quá nhanh/);

  assert.equal(describeAuthError(new ApiError(500, { status: 500, type: "OTHER" }), "fallback"), "fallback");
  assert.match(describeAuthError(new TypeError("offline"), "fallback"), /kết nối/);
});

test("fieldErrorsOf lower-cases field names from validation problems only", () => {
  const problem = new ApiError(422, {
    status: 422,
    type: "VALIDATION_ERROR",
    errors: { CurrentPassword: ["Mật khẩu hiện tại không đúng."] },
  });
  assert.deepEqual(fieldErrorsOf(problem), { currentpassword: "Mật khẩu hiện tại không đúng." });
  assert.deepEqual(fieldErrorsOf(new ApiError(409, { status: 409, type: "X" })), {});
});
