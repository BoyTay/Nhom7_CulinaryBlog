import { ApiError } from "../api/client.ts";

// The API returns error codes in ProblemDetails.type without a localized detail, so the UI owns the wording.
const messagesByCode: Record<string, string> = {
  INVALID_RESET_TOKEN: "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn. Vui lòng yêu cầu liên kết mới.",
  PASSWORD_NOT_SET: "Tài khoản đăng nhập bằng Google chưa có mật khẩu để thay đổi.",
  FILE_REQUIRED: "Vui lòng chọn một tệp ảnh.",
  FILE_EMPTY: "Tệp ảnh rỗng.",
  FILE_SIZE_EXCEEDED: "Ảnh vượt quá giới hạn 5 MB.",
  FILE_MIME_INVALID: "Chỉ chấp nhận ảnh JPEG, PNG, WebP hoặc AVIF.",
  FILE_SIGNATURE_INVALID: "Nội dung tệp không khớp với định dạng ảnh đã khai báo.",
  FILE_STORAGE_UNAVAILABLE: "Kho lưu trữ ảnh tạm thời không khả dụng. Vui lòng thử lại sau.",
  UNAUTHORIZED: "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.",
};

export function describeAuthError(error: unknown, fallback: string): string {
  if (!(error instanceof ApiError)) {
    return "Không thể kết nối đến máy chủ. Vui lòng thử lại sau.";
  }

  if (error.status === 429) {
    return "Bạn thao tác quá nhanh. Vui lòng đợi một phút rồi thử lại.";
  }

  const code = error.problem.type ?? error.problem.errorCode;
  if (code && messagesByCode[code]) {
    return messagesByCode[code];
  }

  if (error.status === 401) {
    return messagesByCode.UNAUTHORIZED;
  }

  return error.problem.detail || fallback;
}

/** Field errors from a 400/422 ProblemDetails, keyed by lower-cased field name. */
export function fieldErrorsOf(error: unknown): Record<string, string> {
  if (!(error instanceof ApiError) || !(error.status === 400 || error.status === 422) || !error.problem.errors) {
    return {};
  }

  return Object.fromEntries(
    Object.entries(error.problem.errors).map(([key, messages]) => [key.toLowerCase(), messages.join(" ")]),
  );
}

/** Mirrors the server password policy so users see problems before submitting. */
export function passwordPolicyError(password: string): string | null {
  if (password.length < 8) return "Mật khẩu phải có ít nhất 8 ký tự.";
  if (!/[A-Z]/.test(password)) return "Mật khẩu phải chứa ít nhất 1 chữ in hoa.";
  if (!/[a-z]/.test(password)) return "Mật khẩu phải chứa ít nhất 1 chữ thường.";
  if (!/[0-9]/.test(password)) return "Mật khẩu phải chứa ít nhất 1 chữ số.";
  if (!/[!*?@#$%^&+=_\-()]/.test(password)) return "Mật khẩu phải chứa ít nhất 1 ký tự đặc biệt.";
  return null;
}
