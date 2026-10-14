"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useState, type FormEvent } from "react";
import { ApiError } from "@/lib/api/client";
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
        if (
          err.problem.errorCode === "ACCOUNT_LOCKED_OUT" ||
          (err.status === 401 && err.problem.detail?.toLowerCase().includes("khóa"))
        ) {
          setIsLockout(true);
          setGeneralError(
            err.problem.detail ||
              "Tài khoản của bạn đã bị khóa tạm thời do nhập sai mật khẩu nhiều lần. Vui lòng thử lại sau 15 phút hoặc liên hệ quản trị viên.",
          );
        } else if (err.status === 401) {
          setGeneralError(err.problem.detail || "Email hoặc mật khẩu không chính xác.");
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

  return (
    <div className="auth-card">
      <div className="auth-card-header">
        <h1 className="auth-title">Đăng nhập</h1>
        <p className="auth-subtitle">Chào mừng bạn quay trở lại với Culinary Blog</p>
      </div>

      {/* Lockout Warning Banner */}
      {isLockout && (
        <div className="auth-banner auth-banner-lockout" role="alert">
          <span className="auth-banner-icon" aria-hidden="true">
            🔒
          </span>
          <div>
            <strong>Tài khoản tạm thời bị khóa:</strong>
            <p style={{ margin: "0.25rem 0 0" }}>{generalError}</p>
          </div>
        </div>
      )}

      {/* Rate Limit Warning Banner */}
      {isRateLimited && !isLockout && (
        <div className="auth-banner auth-banner-warning" role="alert">
          <span className="auth-banner-icon" aria-hidden="true">
            ⏳
          </span>
          <div>
            <strong>Giới hạn yêu cầu:</strong>
            <p style={{ margin: "0.25rem 0 0" }}>{generalError}</p>
          </div>
        </div>
      )}

      {/* Generic Error Banner */}
      {generalError && !isLockout && !isRateLimited && (
        <div className="auth-banner auth-banner-error" role="alert">
          <span className="auth-banner-icon" aria-hidden="true">
            ⚠️
          </span>
          <div>{generalError}</div>
        </div>
      )}

      <form className="auth-form" onSubmit={handleSubmit} noValidate>
        <div className="auth-field">
          <label className="auth-label" htmlFor="login-email">
            Email
          </label>
          <input
            id="login-email"
            name="email"
            type="email"
            autoComplete="email"
            autoFocus
            className="auth-input"
            value={email}
            disabled={isSubmitting}
            aria-invalid={Boolean(fieldErrors.email)}
            aria-describedby={fieldErrors.email ? "login-email-error" : undefined}
            placeholder="ban@example.com"
            onChange={(e) => {
              setEmail(e.target.value);
              if (fieldErrors.email) setFieldErrors((prev) => ({ ...prev, email: undefined }));
            }}
          />
          {fieldErrors.email && (
            <span id="login-email-error" className="auth-field-error" role="alert">
              {fieldErrors.email}
            </span>
          )}
        </div>

        <div className="auth-field">
          <label className="auth-label" htmlFor="login-password">
            Mật khẩu
          </label>
          <input
            id="login-password"
            name="password"
            type="password"
            autoComplete="current-password"
            className="auth-input"
            value={password}
            disabled={isSubmitting}
            aria-invalid={Boolean(fieldErrors.password)}
            aria-describedby={fieldErrors.password ? "login-password-error" : undefined}
            placeholder="••••••••"
            onChange={(e) => {
              setPassword(e.target.value);
              if (fieldErrors.password) setFieldErrors((prev) => ({ ...prev, password: undefined }));
            }}
          />
          {fieldErrors.password && (
            <span id="login-password-error" className="auth-field-error" role="alert">
              {fieldErrors.password}
            </span>
          )}
        </div>

        <button type="submit" className="auth-submit-btn" disabled={isSubmitting}>
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

      <div className="auth-divider">
        <span>hoặc</span>
      </div>

      <GoogleSignInButton
        text="signin_with"
        disabled={isSubmitting}
        onSuccess={() => router.push(returnUrl)}
        onError={(err, lockout) => {
          setGeneralError(err);
          setIsLockout(Boolean(lockout));
        }}
      />

      <div className="auth-footer">
        Chưa có tài khoản?{" "}
        <Link
          href={returnUrl !== "/" ? `/register?returnUrl=${encodeURIComponent(returnUrl)}` : "/register"}
          className="auth-link"
        >
          Đăng ký ngay
        </Link>
      </div>
    </div>
  );
}

