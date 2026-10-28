"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useState, type FormEvent } from "react";
import { resetPasswordApi } from "@/lib/auth/auth-client";
import { describeAuthError, fieldErrorsOf, passwordPolicyError } from "@/lib/auth/auth-errors";

export function ResetPasswordForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const email = searchParams.get("email") ?? "";
  const token = searchParams.get("token") ?? "";

  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<{ newPassword?: string; confirmPassword?: string }>({});
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  if (!email || !token) {
    return (
      <div className="auth-form-card">
        <div className="auth-header-block">
          <h1 className="auth-heading">Liên kết không hợp lệ</h1>
          <p className="auth-subheading">
            Liên kết đặt lại mật khẩu bị thiếu thông tin. Hãy mở đúng liên kết trong email hoặc yêu cầu liên kết mới.
          </p>
        </div>
        <Link href="/forgot-password" className="auth-btn-primary">
          Yêu cầu liên kết mới
        </Link>
      </div>
    );
  }

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setErrorMessage(null);

    const errors: typeof fieldErrors = {};
    const policyError = passwordPolicyError(newPassword);
    if (policyError) errors.newPassword = policyError;
    if (newPassword !== confirmPassword) errors.confirmPassword = "Mật khẩu xác nhận không khớp.";
    setFieldErrors(errors);
    if (Object.keys(errors).length > 0) return;

    setIsSubmitting(true);
    try {
      await resetPasswordApi({ email, token, newPassword });
      router.replace("/login?notice=password-reset");
    } catch (err) {
      const serverErrors = fieldErrorsOf(err);
      if (serverErrors.newpassword) {
        setFieldErrors({ newPassword: serverErrors.newpassword });
      } else {
        setErrorMessage(describeAuthError(err, "Không thể đặt lại mật khẩu. Vui lòng thử lại."));
      }
      setIsSubmitting(false);
    }
  };

  return (
    <div className="auth-form-card">
      <div className="auth-header-block">
        <h1 className="auth-heading">Đặt mật khẩu mới</h1>
        <p className="auth-subheading">
          Tạo mật khẩu mới cho <strong>{email}</strong>. Mọi phiên đăng nhập cũ sẽ bị đăng xuất.
        </p>
      </div>

      <form className="auth-form-inner" onSubmit={handleSubmit} noValidate>
        <div className="auth-field-group">
          <label className="auth-field-label" htmlFor="rp-new">
            Mật khẩu mới
          </label>
          <div className="auth-input-wrapper">
            <input
              id="rp-new"
              type={showPassword ? "text" : "password"}
              autoComplete="new-password"
              className="auth-input-control auth-input-has-toggle"
              value={newPassword}
              disabled={isSubmitting}
              placeholder="Ít nhất 8 ký tự, có hoa, thường, số, ký tự đặc biệt"
              aria-invalid={Boolean(fieldErrors.newPassword)}
              aria-describedby={fieldErrors.newPassword ? "rp-new-error" : undefined}
              onChange={(e) => setNewPassword(e.target.value)}
              autoFocus
            />
            <button
              type="button"
              className="auth-password-toggle-btn"
              onClick={() => setShowPassword((prev) => !prev)}
              aria-label={showPassword ? "Ẩn mật khẩu" : "Hiện mật khẩu"}
              aria-pressed={showPassword}
            >
              {showPassword ? "Ẩn" : "Hiện"}
            </button>
          </div>
          {fieldErrors.newPassword && (
            <span id="rp-new-error" className="auth-field-error" role="alert">
              <span>{fieldErrors.newPassword}</span>
            </span>
          )}
        </div>

        <div className="auth-field-group">
          <label className="auth-field-label" htmlFor="rp-confirm">
            Xác nhận mật khẩu mới
          </label>
          <input
            id="rp-confirm"
            type={showPassword ? "text" : "password"}
            autoComplete="new-password"
            className="auth-input-control"
            value={confirmPassword}
            disabled={isSubmitting}
            placeholder="Nhập lại mật khẩu mới"
            aria-invalid={Boolean(fieldErrors.confirmPassword)}
            aria-describedby={fieldErrors.confirmPassword ? "rp-confirm-error" : undefined}
            onChange={(e) => setConfirmPassword(e.target.value)}
          />
          {fieldErrors.confirmPassword && (
            <span id="rp-confirm-error" className="auth-field-error" role="alert">
              <span>{fieldErrors.confirmPassword}</span>
            </span>
          )}
        </div>

        {errorMessage && (
          <div className="auth-alert auth-alert-error" role="alert">
            <div className="auth-alert-content">
              <span>{errorMessage}</span>
              <Link href="/forgot-password">Yêu cầu liên kết mới</Link>
            </div>
          </div>
        )}

        <button type="submit" className="auth-btn-primary" disabled={isSubmitting}>
          {isSubmitting ? (
            <>
              <span className="auth-spinner" aria-hidden="true" />
              <span>Đang cập nhật...</span>
            </>
          ) : (
            <span>Đặt lại mật khẩu</span>
          )}
        </button>
      </form>

      <Link href="/login" className="auth-back-link">
        ← Quay lại đăng nhập
      </Link>
    </div>
  );
}
