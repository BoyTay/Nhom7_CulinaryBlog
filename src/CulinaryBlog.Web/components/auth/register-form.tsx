"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useState, type FormEvent } from "react";
import { ApiError } from "@/lib/api/client";
import { useAuth } from "@/lib/auth/use-auth";
import { GoogleSignInButton } from "@/components/auth/google-sign-in-button";

export function RegisterForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const returnUrl = searchParams.get("returnUrl") || "/";

  const { register } = useAuth();

  const [displayName, setDisplayName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const [fieldErrors, setFieldErrors] = useState<{
    displayName?: string;
    email?: string;
    password?: string;
    confirmPassword?: string;
  }>({});
  const [generalError, setGeneralError] = useState<string | null>(null);
  const [successNotice, setSuccessNotice] = useState<string | null>(null);

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
        } else if (err.status === 400 && err.problem.errors) {
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

      <h1 className="auth-heading">Tạo tài khoản</h1>

      <form className="auth-form-inner" onSubmit={handleSubmit} noValidate>
        {/* Display Name Field */}
        <div className="auth-field-group">
          <label className="auth-field-label" htmlFor="dn">
            Tên hiển thị
          </label>
          <input
            id="dn"
            name="displayName"
            type="text"
            autoComplete="name"
            autoFocus
            className="auth-input-control"
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
          <input
            id="em"
            name="email"
            type="email"
            autoComplete="email"
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
            autoComplete="new-password"
            className="auth-input-control"
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
          <span id="pw-hint" className="auth-field-hint">
            Tối thiểu 8 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt.
          </span>
          {fieldErrors.password && (
            <span id="pw-error" className="auth-field-error" role="alert">
              {fieldErrors.password}
            </span>
          )}
        </div>

        {/* Confirm Password Field */}
        <div className="auth-field-group">
          <label className="auth-field-label" htmlFor="cpw">
            Xác nhận mật khẩu
          </label>
          <input
            id="cpw"
            name="confirmPassword"
            type="password"
            autoComplete="new-password"
            className="auth-input-control"
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
          {fieldErrors.confirmPassword && (
            <span id="cpw-error" className="auth-field-error" role="alert">
              {fieldErrors.confirmPassword}
            </span>
          )}
        </div>

        {/* Success Notice */}
        {successNotice && (
          <div className="auth-alert auth-alert-success" role="status">
            {successNotice}
          </div>
        )}

        {/* Error Alert */}
        {generalError && (
          <div className="auth-alert auth-alert-error" role="alert">
            {generalError}
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
    </div>
  );
}
