"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useState, type FormEvent } from "react";
import { ApiError } from "@/lib/api/client";
import { isAccountLocked, safeReturnUrl } from "@/lib/auth/session";
import { useAuth } from "@/lib/auth/use-auth";
import { GoogleSignInButton } from "@/components/auth/google-sign-in-button";

const noticeMessages: Record<string, string> = {
  "password-changed": "Đã đổi mật khẩu. Mọi phiên cũ đã bị đăng xuất, vui lòng đăng nhập bằng mật khẩu mới.",
  "password-reset": "Đặt lại mật khẩu thành công. Vui lòng đăng nhập bằng mật khẩu mới.",
};

export function LoginForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const returnUrl = safeReturnUrl(searchParams.get("returnUrl"));

  const notice = noticeMessages[searchParams.get("notice") ?? ""];

  const { login } = useAuth();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const [fieldErrors, setFieldErrors] = useState<{ email?: string; password?: string }>({});
  const [generalError, setGeneralError] = useState<string | null>(null);
  const [isLockout, setIsLockout] = useState(false);
  const [isRateLimited, setIsRateLimited] = useState(false);

  const validateForm = (): boolean => {
    const errors: { email?: string; password?: string } = {};

    const trimmedEmail = email.trim();
    if (!trimmedEmail) {
      errors.email = "Vui lòng nhập địa chỉ email.";
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(trimmedEmail)) {
      errors.email = "Địa chỉ email không đúng định dạng.";
    }

    if (!password) {
      errors.password = "Vui lòng nhập mật khẩu.";
    }

    setFieldErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setGeneralError(null);
    setIsLockout(false);
    setIsRateLimited(false);

    if (!validateForm()) {
      return;
    }

    setIsSubmitting(true);
    try {
      await login({ email: email.trim(), password });
      router.push(returnUrl);
    } catch (err) {
      if (err instanceof ApiError) {
        if (isAccountLocked(err.problem)) {
          setIsLockout(true);
          setGeneralError(
            err.problem.detail ||
              "Tài khoản của bạn đã bị tạm khóa do nhập sai mật khẩu nhiều lần. Vui lòng thử lại sau 15 phút hoặc liên hệ quản trị viên.",
          );
        } else if (err.status === 401) {
          setGeneralError(err.problem.detail || "Email hoặc mật khẩu không chính xác.");
        } else if (err.status === 429) {
          setIsRateLimited(true);
          setGeneralError(
            "Bạn đã thực hiện quá nhiều thao tác đăng nhập. Vui lòng đợi trong giây lát rồi thử lại.",
          );
        } else if ((err.status === 400 || err.status === 422) && err.problem.errors) {
          const serverErrors: { email?: string; password?: string } = {};
          for (const [key, messages] of Object.entries(err.problem.errors)) {
            const k = key.toLowerCase();
            if (k === "email") {
              serverErrors.email = messages.join(" ");
            } else if (k === "password") {
              serverErrors.password = messages.join(" ");
            }
          }
          setFieldErrors(serverErrors);
          setGeneralError("Vui lòng kiểm tra lại thông tin đăng nhập.");
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

  const registerHref =
    returnUrl !== "/"
      ? `/register?returnUrl=${encodeURIComponent(returnUrl)}`
      : "/register";

  return (
    <div className="auth-form-card">
      {/* Mode Tab Switcher */}
      <div role="tablist" aria-label="Chế độ" className="auth-tablist">
        <span role="tab" aria-selected="true" className="auth-tab auth-tab-active">
          Đăng nhập
        </span>
        <Link
          role="tab"
          aria-selected="false"
          href={registerHref}
          className="auth-tab auth-tab-inactive"
        >
          Đăng ký
        </Link>
      </div>

      <div className="auth-header-block">
        <h1 className="auth-heading">Chào mừng trở lại</h1>
        <p className="auth-subheading">Đăng nhập để tiếp tục nấu và chia sẻ.</p>
      </div>

      <form className="auth-form-inner" onSubmit={handleSubmit} noValidate>
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
              autoFocus
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
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
            <label className="auth-field-label" htmlFor="pw">
              Mật khẩu
            </label>
            <Link
              href="/forgot-password"
              style={{ fontSize: "13px", color: "#c2410c", fontWeight: 500, textDecoration: "none" }}
            >
              Quên mật khẩu?
            </Link>
          </div>
          <div className="auth-input-wrapper">
            <svg className="auth-input-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
              <rect x="3" y="11" width="18" height="11" rx="2" ry="2" />
              <path d="M7 11V7a5 5 0 0 1 10 0v4" />
            </svg>
            <input
              id="pw"
              name="password"
              type={showPassword ? "text" : "password"}
              autoComplete="current-password"
              className="auth-input-control auth-input-has-icon auth-input-has-toggle"
              value={password}
              disabled={isSubmitting}
              aria-invalid={Boolean(fieldErrors.password)}
              aria-describedby={fieldErrors.password ? "pw-error" : undefined}
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

        {notice && !generalError && (
          <div className="auth-alert auth-alert-success" role="status">
            <div className="auth-alert-content">
              <span>{notice}</span>
            </div>
          </div>
        )}

        {/* Lockout / Rate limit / Error Alert */}
        {isLockout && (
          <div className="auth-alert auth-alert-lockout" role="alert">
            <svg className="auth-alert-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
              <rect x="3" y="11" width="18" height="11" rx="2" ry="2" />
              <path d="M7 11V7a5 5 0 0 1 10 0v4" />
            </svg>
            <div className="auth-alert-content">
              <strong>Tài khoản tạm thời bị khóa:</strong>
              <span>{generalError}</span>
            </div>
          </div>
        )}

        {isRateLimited && !isLockout && (
          <div className="auth-alert auth-alert-warning" role="alert">
            <svg className="auth-alert-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
              <circle cx="12" cy="12" r="10" />
              <line x1="12" y1="8" x2="12" y2="12" />
              <line x1="12" y1="16" x2="12.01" y2="16" />
            </svg>
            <div className="auth-alert-content">
              <strong>Giới hạn yêu cầu:</strong>
              <span>{generalError}</span>
            </div>
          </div>
        )}

        {generalError && !isLockout && !isRateLimited && (
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
              <span>Đang đăng nhập...</span>
            </>
          ) : (
            <span>Đăng nhập</span>
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
        text="signin_with"
        disabled={isSubmitting}
        onSuccess={() => router.push(returnUrl)}
        onError={(err, lockout) => {
          setGeneralError(err);
          setIsLockout(Boolean(lockout));
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
