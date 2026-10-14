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
    } else if (trimmedName.length < 2 || trimmedName.length > 50) {
      errors.displayName = "Tên hiển thị phải từ 2 đến 50 ký tự.";
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
          setGeneralError("Địa chỉ email đã được đăng ký trước đó.");
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

  return (
    <div className="auth-card">
      <div className="auth-card-header">
        <h1 className="auth-title">Đăng ký tài khoản</h1>
        <p className="auth-subtitle">Gia nhập cộng đồng ẩm thực Culinary Blog ngay hôm nay</p>
      </div>

      {/* Success Notification */}
      {successNotice && (
        <div className="auth-banner auth-banner-success" role="alert">
          <span className="auth-banner-icon" aria-hidden="true">
            ✅
          </span>
          <div>{successNotice}</div>
        </div>
      )}

      {/* Error Notification */}
      {generalError && (
        <div className="auth-banner auth-banner-error" role="alert">
          <span className="auth-banner-icon" aria-hidden="true">
            ⚠️
          </span>
          <div>{generalError}</div>
        </div>
      )}

      <form className="auth-form" onSubmit={handleSubmit} noValidate>
        <div className="auth-field">
          <label className="auth-label" htmlFor="register-name">
            Tên hiển thị
          </label>
          <input
            id="register-name"
            name="displayName"
            type="text"
            autoComplete="name"
            autoFocus
            className="auth-input"
            value={displayName}
            disabled={isSubmitting}
            aria-invalid={Boolean(fieldErrors.displayName)}
            aria-describedby={fieldErrors.displayName ? "register-name-error" : undefined}
            placeholder="Nguyễn Văn A"
            onChange={(e) => {
              setDisplayName(e.target.value);
              if (fieldErrors.displayName) {
                setFieldErrors((prev) => ({ ...prev, displayName: undefined }));
              }
            }}
          />
          {fieldErrors.displayName && (
            <span id="register-name-error" className="auth-field-error" role="alert">
              {fieldErrors.displayName}
            </span>
          )}
        </div>

        <div className="auth-field">
          <label className="auth-label" htmlFor="register-email">
            Email
          </label>
          <input
            id="register-email"
            name="email"
            type="email"
            autoComplete="email"
            className="auth-input"
            value={email}
            disabled={isSubmitting}
            aria-invalid={Boolean(fieldErrors.email)}
            aria-describedby={fieldErrors.email ? "register-email-error" : undefined}
            placeholder="ban@example.com"
            onChange={(e) => {
              setEmail(e.target.value);
              if (fieldErrors.email) {
                setFieldErrors((prev) => ({ ...prev, email: undefined }));
              }
            }}
          />
          {fieldErrors.email && (
            <span id="register-email-error" className="auth-field-error" role="alert">
              {fieldErrors.email}
            </span>
          )}
        </div>

        <div className="auth-field">
          <label className="auth-label" htmlFor="register-password">
            Mật khẩu
          </label>
          <input
            id="register-password"
            name="password"
            type="password"
            autoComplete="new-password"
            className="auth-input"
            value={password}
            disabled={isSubmitting}
            aria-invalid={Boolean(fieldErrors.password)}
            aria-describedby={
              fieldErrors.password ? "register-password-error" : "register-password-hint"
            }
            placeholder="Tối thiểu 8 ký tự phức tạp"
            onChange={(e) => {
              setPassword(e.target.value);
              if (fieldErrors.password) {
                setFieldErrors((prev) => ({ ...prev, password: undefined }));
              }
            }}
          />
          {fieldErrors.password ? (
            <span id="register-password-error" className="auth-field-error" role="alert">
              {fieldErrors.password}
            </span>
          ) : (
            <span id="register-password-hint" className="auth-field-hint">
              Ít nhất 8 ký tự, gồm chữ hoa, chữ thường, chữ số và ký tự đặc biệt.
            </span>
          )}
        </div>

        <div className="auth-field">
          <label className="auth-label" htmlFor="register-confirm-password">
            Xác nhận mật khẩu
          </label>
          <input
            id="register-confirm-password"
            name="confirmPassword"
            type="password"
            autoComplete="new-password"
            className="auth-input"
            value={confirmPassword}
            disabled={isSubmitting}
            aria-invalid={Boolean(fieldErrors.confirmPassword)}
            aria-describedby={
              fieldErrors.confirmPassword ? "register-confirm-password-error" : undefined
            }
            placeholder="Nhập lại mật khẩu"
            onChange={(e) => {
              setConfirmPassword(e.target.value);
              if (fieldErrors.confirmPassword) {
                setFieldErrors((prev) => ({ ...prev, confirmPassword: undefined }));
              }
            }}
          />
          {fieldErrors.confirmPassword && (
            <span id="register-confirm-password-error" className="auth-field-error" role="alert">
              {fieldErrors.confirmPassword}
            </span>
          )}
        </div>

        <button type="submit" className="auth-submit-btn" disabled={isSubmitting}>
          {isSubmitting ? (
            <>
              <span className="auth-spinner" aria-hidden="true" />
              <span>Đang tạo tài khoản...</span>
            </>
          ) : (
            <span>Tạo tài khoản</span>
          )}
        </button>
      </form>

      <div className="auth-divider">
        <span>hoặc</span>
      </div>

      <GoogleSignInButton
        text="signup_with"
        disabled={isSubmitting}
        onSuccess={() => router.push(returnUrl)}
        onError={(err) => setGeneralError(err)}
      />

      <div className="auth-footer">
        Đã có tài khoản?{" "}
        <Link
          href={returnUrl !== "/" ? `/login?returnUrl=${encodeURIComponent(returnUrl)}` : "/login"}
          className="auth-link"
        >
          Đăng nhập ngay
        </Link>
      </div>
    </div>
  );
}

