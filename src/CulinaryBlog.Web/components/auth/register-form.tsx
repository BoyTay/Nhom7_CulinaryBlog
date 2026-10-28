"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useState, type FormEvent } from "react";
import { ApiError } from "@/lib/api/client";
import { safeReturnUrl } from "@/lib/auth/session";
import { useAuth } from "@/lib/auth/use-auth";
import { GoogleSignInButton } from "@/components/auth/google-sign-in-button";

export function RegisterForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const returnUrl = safeReturnUrl(searchParams.get("returnUrl"));

  const { register } = useAuth();

  const [displayName, setDisplayName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const [fieldErrors, setFieldErrors] = useState<{
    displayName?: string;
    email?: string;
    password?: string;
    confirmPassword?: string;
  }>({});
  const [generalError, setGeneralError] = useState<string | null>(null);
  const [successNotice, setSuccessNotice] = useState<string | null>(null);

  // Live password checklist rules
  const hasMinLen = password.length >= 8;
  const hasUpperLower = /[A-Z]/.test(password) && /[a-z]/.test(password);
  const hasNumber = /[0-9]/.test(password);
  const hasSpecial = /[^a-zA-Z0-9]/.test(password);

  const validateForm = (): boolean => {
    const errors: {
      displayName?: string;
      email?: string;
      password?: string;
      confirmPassword?: string;
    } = {};

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

    setFieldErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setGeneralError(null);
    setSuccessNotice(null);

    if (!validateForm()) {
      return;
    }

    setIsSubmitting(true);
    try {
      await register({
        displayName: displayName.trim(),
        email: email.trim(),
        password,
      });

      setSuccessNotice("Đăng ký tài khoản thành công! Đang chuyển hướng...");
      setTimeout(() => {
        router.push(returnUrl);
      }, 700);
    } catch (err) {
      if (err instanceof ApiError) {
        if (
          err.status === 409 ||
          err.problem.type === "EMAIL_ALREADY_EXISTS" ||
          err.problem.detail?.toLowerCase().includes("tồn tại")
        ) {
          setFieldErrors((prev) => ({
            ...prev,
            email: "Email này đã được sử dụng. Vui lòng chọn email khác hoặc đăng nhập.",
          }));
          setGeneralError("Email này đã được đăng ký.");
        } else if (err.status === 429) {
          setGeneralError("Bạn đã gửi quá nhiều yêu cầu. Vui lòng đợi trong giây lát rồi thử lại.");
        } else if ((err.status === 400 || err.status === 422) && err.problem.errors) {
          const serverErrors: typeof fieldErrors = {};
          for (const [key, messages] of Object.entries(err.problem.errors)) {
            const k = key.toLowerCase();
            if (k.includes("name") || k.includes("display")) {
              serverErrors.displayName = messages.join(" ");
            } else if (k.includes("email")) {
              serverErrors.email = messages.join(" ");
            } else if (k.includes("pass")) {
              serverErrors.password = messages.join(" ");
            }
          }
          setFieldErrors(serverErrors);
          setGeneralError("Vui lòng kiểm tra lại các trường thông tin được đánh dấu.");
        } else {
          setGeneralError(err.problem.detail || err.message);
        }
      } else {
        setGeneralError("Không thể kết nối đến máy chủ. Vui lòng kiểm tra lại kết nối mạng.");
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  const loginHref =
    returnUrl !== "/" ? `/login?returnUrl=${encodeURIComponent(returnUrl)}` : "/login";

  return (
    <div className="auth-form-card">
      {/* Mode Tab Switcher */}
      <div role="tablist" aria-label="Chế độ" className="auth-tablist">
        <Link
          role="tab"
          aria-selected="false"
          href={loginHref}
          className="auth-tab auth-tab-inactive"
        >
          Đăng nhập
        </Link>
        <span role="tab" aria-selected="true" className="auth-tab auth-tab-active">
          Đăng ký
        </span>
      </div>

      <div className="auth-header-block">
        <h1 className="auth-heading">Tạo tài khoản</h1>
        <p className="auth-subheading">Gia nhập cộng đồng hơn 15.000+ người yêu bếp để sẻ chia và lưu trữ công thức.</p>
      </div>

      <form className="auth-form-inner" onSubmit={handleSubmit} noValidate>
        {/* Display Name Field */}
        <div className="auth-field-group">
          <label className="auth-field-label" htmlFor="dn">
            Tên hiển thị
          </label>
          <div className="auth-input-wrapper">
            <svg className="auth-input-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
              <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" />
              <circle cx="12" cy="7" r="4" />
            </svg>
            <input
              id="dn"
              name="displayName"
              type="text"
              autoComplete="name"
              autoFocus
              className="auth-input-control auth-input-has-icon"
              value={displayName}
              disabled={isSubmitting}
              aria-invalid={Boolean(fieldErrors.displayName)}
              aria-describedby={fieldErrors.displayName ? "dn-error" : undefined}
              placeholder="2–100 ký tự"
              onChange={(e) => {
                setDisplayName(e.target.value);
                if (fieldErrors.displayName) {
                  setFieldErrors((prev) => ({ ...prev, displayName: undefined }));
                }
              }}
            />
          </div>
          {fieldErrors.displayName && (
            <span id="dn-error" className="auth-field-error" role="alert">
              {fieldErrors.displayName}
            </span>
          )}
        </div>

        {/* Email Field */}
        <div className="auth-field-group">
          <label className="auth-field-label" htmlFor="em">
            Email
          </label>
          <div className="auth-input-wrapper">
            <svg className="auth-input-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
              <rect x="2" y="4" width="20" height="16" rx="2" />
              <path d="m22 7-8.97 5.7a1.94 1.94 0 0 1-2.06 0L2 7" />
            </svg>
            <input
              id="em"
              name="email"
              type="email"
              autoComplete="email"
              className="auth-input-control auth-input-has-icon"
              value={email}
              disabled={isSubmitting}
              aria-invalid={Boolean(fieldErrors.email)}
              aria-describedby={fieldErrors.email ? "em-error" : undefined}
              placeholder="ban@email.com"
              onChange={(e) => {
                setEmail(e.target.value);
                if (fieldErrors.email) setFieldErrors((prev) => ({ ...prev, email: undefined }));
              }}
            />
          </div>
          {fieldErrors.email && (
            <span id="em-error" className="auth-field-error" role="alert">
              {fieldErrors.email}
            </span>
          )}
        </div>

        {/* Password Field */}
        <div className="auth-field-group">
          <label className="auth-field-label" htmlFor="pw">
            Mật khẩu
          </label>
          <div className="auth-input-wrapper">
            <svg className="auth-input-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
              <rect x="3" y="11" width="18" height="11" rx="2" ry="2" />
              <path d="M7 11V7a5 5 0 0 1 10 0v4" />
            </svg>
            <input
              id="pw"
              name="password"
              type={showPassword ? "text" : "password"}
              autoComplete="new-password"
              className="auth-input-control auth-input-has-icon auth-input-has-toggle"
              value={password}
              disabled={isSubmitting}
              aria-invalid={Boolean(fieldErrors.password)}
              aria-describedby="pw-hint"
              placeholder="••••••••"
              onChange={(e) => {
                setPassword(e.target.value);
                if (fieldErrors.password) setFieldErrors((prev) => ({ ...prev, password: undefined }));
              }}
            />
            <button
              type="button"
              className="auth-password-toggle-btn"
              onClick={() => setShowPassword((prev) => !prev)}
              aria-label={showPassword ? "Ẩn mật khẩu" : "Hiện mật khẩu"}
              aria-pressed={showPassword}
            >
              {showPassword ? (
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                  <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24" />
                  <line x1="1" y1="1" x2="23" y2="23" />
                </svg>
              ) : (
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                  <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" />
                  <circle cx="12" cy="12" r="3" />
                </svg>
              )}
            </button>
          </div>

          {/* Real-time password criteria checklist */}
          <div className="auth-checklist" id="pw-hint">
            <div className={`auth-checklist-item ${hasMinLen ? "auth-checklist-item-valid" : ""}`}>
              <svg width="14" height="14" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                <path fillRule="evenodd" d="M16.704 4.153a.75.75 0 01.143 1.052l-8 10.5a.75.75 0 01-1.127.075l-4.5-4.5a.75.75 0 011.06-1.06l3.894 3.893 7.48-9.817a.75.75 0 011.05-.143z" clipRule="evenodd" />
              </svg>
              <span>Tối thiểu 8 ký tự</span>
            </div>
            <div className={`auth-checklist-item ${hasUpperLower ? "auth-checklist-item-valid" : ""}`}>
              <svg width="14" height="14" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                <path fillRule="evenodd" d="M16.704 4.153a.75.75 0 01.143 1.052l-8 10.5a.75.75 0 01-1.127.075l-4.5-4.5a.75.75 0 011.06-1.06l3.894 3.893 7.48-9.817a.75.75 0 011.05-.143z" clipRule="evenodd" />
              </svg>
              <span>Chữ hoa và chữ thường</span>
            </div>
            <div className={`auth-checklist-item ${hasNumber && hasSpecial ? "auth-checklist-item-valid" : ""}`}>
              <svg width="14" height="14" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                <path fillRule="evenodd" d="M16.704 4.153a.75.75 0 01.143 1.052l-8 10.5a.75.75 0 01-1.127.075l-4.5-4.5a.75.75 0 011.06-1.06l3.894 3.893 7.48-9.817a.75.75 0 011.05-.143z" clipRule="evenodd" />
              </svg>
              <span>Chứa chữ số và ký tự đặc biệt</span>
            </div>
          </div>

          {fieldErrors.password && (
            <span id="pw-error" className="auth-field-error" role="alert">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
                <circle cx="12" cy="12" r="10" />
                <line x1="12" y1="8" x2="12" y2="12" />
                <line x1="12" y1="16" x2="12.01" y2="16" />
              </svg>
              <span>{fieldErrors.password}</span>
            </span>
          )}
        </div>

        {/* Confirm Password Field */}
        <div className="auth-field-group">
          <label className="auth-field-label" htmlFor="cpw">
            Xác nhận mật khẩu
          </label>
          <div className="auth-input-wrapper">
            <svg className="auth-input-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
              <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" />
            </svg>
            <input
              id="cpw"
              name="confirmPassword"
              type={showConfirmPassword ? "text" : "password"}
              autoComplete="new-password"
              className="auth-input-control auth-input-has-icon auth-input-has-toggle"
              value={confirmPassword}
              disabled={isSubmitting}
              aria-invalid={Boolean(fieldErrors.confirmPassword)}
              aria-describedby={fieldErrors.confirmPassword ? "cpw-error" : undefined}
              placeholder="••••••••"
              onChange={(e) => {
                setConfirmPassword(e.target.value);
                if (fieldErrors.confirmPassword) {
                  setFieldErrors((prev) => ({ ...prev, confirmPassword: undefined }));
                }
              }}
            />
            <button
              type="button"
              className="auth-password-toggle-btn"
              onClick={() => setShowConfirmPassword((prev) => !prev)}
              aria-label={showConfirmPassword ? "Ẩn xác nhận mật khẩu" : "Hiện xác nhận mật khẩu"}
              aria-pressed={showConfirmPassword}
            >
              {showConfirmPassword ? (
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                  <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24" />
                  <line x1="1" y1="1" x2="23" y2="23" />
                </svg>
              ) : (
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                  <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" />
                  <circle cx="12" cy="12" r="3" />
                </svg>
              )}
            </button>
          </div>
          {fieldErrors.confirmPassword && (
            <span id="cpw-error" className="auth-field-error" role="alert">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
                <circle cx="12" cy="12" r="10" />
                <line x1="12" y1="8" x2="12" y2="12" />
                <line x1="12" y1="16" x2="12.01" y2="16" />
              </svg>
              <span>{fieldErrors.confirmPassword}</span>
            </span>
          )}
        </div>

        {/* Success Notice */}
        {successNotice && (
          <div className="auth-alert auth-alert-success" role="status">
            <svg className="auth-alert-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
              <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14" />
              <polyline points="22 4 12 14.01 9 11.01" />
            </svg>
            <div className="auth-alert-content">
              <span>{successNotice}</span>
            </div>
          </div>
        )}

        {/* Error Alert */}
        {generalError && (
          <div className="auth-alert auth-alert-error" role="alert">
            <svg className="auth-alert-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
              <circle cx="12" cy="12" r="10" />
              <line x1="15" y1="9" x2="9" y2="15" />
              <line x1="9" y1="9" x2="15" y2="15" />
            </svg>
            <div className="auth-alert-content">
              <span>{generalError}</span>
            </div>
          </div>
        )}

        {/* Submit Button */}
        <button type="submit" className="auth-btn-primary" disabled={isSubmitting}>
          {isSubmitting ? (
            <>
              <span className="auth-spinner" aria-hidden="true" />
              <span>Đang tạo tài khoản...</span>
            </>
          ) : (
            <span>Đăng ký</span>
          )}
        </button>
      </form>

      {/* Divider */}
      <div className="auth-divider-wrap" aria-hidden="true">
        <span className="auth-divider-line" />
        <span>hoặc</span>
        <span className="auth-divider-line" />
      </div>

      {/* Google Sign-In */}
      <GoogleSignInButton
        text="signup_with"
        disabled={isSubmitting}
        onSuccess={() => router.push(returnUrl)}
        onError={(err) => {
          setGeneralError(err);
        }}
      />

      {/* Footer Navigation */}
      <div className="auth-footer-nav">
        <Link href="/" className="auth-back-link">
          ← Về trang chủ Culinary Blog
        </Link>
      </div>
    </div>
  );
}
