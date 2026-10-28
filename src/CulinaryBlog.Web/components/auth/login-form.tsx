"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useState, type FormEvent } from "react";
import { ApiError } from "@/lib/api/client";
import { isAccountLocked } from "@/lib/auth/session";
import { useAuth } from "@/lib/auth/use-auth";
import { GoogleSignInButton } from "@/components/auth/google-sign-in-button";

export function LoginForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const returnUrl = searchParams.get("returnUrl") || "/";

  const { login } = useAuth();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
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
              "Tài khoản của bạn đã bị khóa tạm thời do nhập sai mật khẩu nhiều lần. Vui lòng thử lại sau 15 phút hoặc liên hệ quản trị viên.",
          );
        } else if (err.status === 401) {
          setGeneralError(err.problem.detail || "Email hoặc mật khẩu không đúng.");
        } else if (err.status === 429) {
          setIsRateLimited(true);
          setGeneralError(
            "Bạn đã thực hiện quá nhiều thao tác đăng nhập. Vui lòng đợi trong giây lát rồi thử lại.",
          );
        } else if (err.status === 400 && err.problem.errors) {
          const serverErrors: { email?: string; password?: string } = {};
          for (const [key, messages] of Object.entries(err.problem.errors)) {
            if (key.toLowerCase() === "email") {
              serverErrors.email = messages.join(" ");
            } else if (key.toLowerCase() === "password") {
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

      <h1 className="auth-heading">Chào mừng trở lại</h1>

      <form className="auth-form-inner" onSubmit={handleSubmit} noValidate>
        {/* Email Field */}
        <div className="auth-field-group">
          <label className="auth-field-label" htmlFor="em">
            Email
          </label>
          <input
            id="em"
            name="email"
            type="email"
            autoComplete="email"
            autoFocus
            className="auth-input-control"
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
          <input
            id="pw"
            name="password"
            type="password"
            autoComplete="current-password"
            className="auth-input-control"
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
          {fieldErrors.password && (
            <span id="pw-error" className="auth-field-error" role="alert">
              {fieldErrors.password}
            </span>
          )}
        </div>

        {/* Lockout / Rate limit / Error Alert */}
        {isLockout && (
          <div className="auth-alert auth-alert-lockout" role="alert">
            <strong>Tài khoản tạm thời bị khóa:</strong>
            <span>{generalError}</span>
          </div>
        )}

        {isRateLimited && !isLockout && (
          <div className="auth-alert auth-alert-warning" role="alert">
            <strong>Giới hạn yêu cầu:</strong>
            <span>{generalError}</span>
          </div>
        )}

        {generalError && !isLockout && !isRateLimited && (
          <div className="auth-alert auth-alert-error" role="alert">
            {generalError}
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
    </div>
  );
}
