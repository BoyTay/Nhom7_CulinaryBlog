"use client";

import Link from "next/link";
import { useState, type FormEvent } from "react";
import { forgotPasswordApi } from "@/lib/auth/auth-client";
import { describeAuthError, fieldErrorsOf } from "@/lib/auth/auth-errors";

const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export function ForgotPasswordForm() {
  const [email, setEmail] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [emailError, setEmailError] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [sentMessage, setSentMessage] = useState<string | null>(null);

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setErrorMessage(null);

    const trimmedEmail = email.trim();
    if (!trimmedEmail) {
      setEmailError("Vui lòng nhập địa chỉ email.");
      return;
    }
    if (!emailPattern.test(trimmedEmail)) {
      setEmailError("Địa chỉ email không đúng định dạng.");
      return;
    }

    setIsSubmitting(true);
    try {
      const response = await forgotPasswordApi({ email: trimmedEmail });
      setSentMessage(response.message);
    } catch (err) {
      const serverEmailError = fieldErrorsOf(err).email;
      if (serverEmailError) {
        setEmailError(serverEmailError);
      } else {
        setErrorMessage(describeAuthError(err, "Không thể gửi yêu cầu. Vui lòng thử lại."));
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  if (sentMessage) {
    return (
      <div className="auth-form-card">
        <div className="auth-header-block">
          <h1 className="auth-heading">Kiểm tra hộp thư của bạn</h1>
          <p className="auth-subheading">
            Liên kết đặt lại mật khẩu có hiệu lực trong 30 phút và chỉ dùng được một lần.
          </p>
        </div>
        <div className="auth-alert auth-alert-success" role="status">
          <svg className="auth-alert-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
            <rect x="2" y="4" width="20" height="16" rx="2" />
            <path d="m22 7-8.97 5.7a1.94 1.94 0 0 1-2.06 0L2 7" />
          </svg>
          <div className="auth-alert-content">
            <span>{sentMessage}</span>
          </div>
        </div>
        <button
          type="button"
          className="auth-btn-primary"
          onClick={() => setSentMessage(null)}
        >
          Gửi lại liên kết
        </button>
        <Link href="/login" className="auth-back-link">
          ← Quay lại đăng nhập
        </Link>
      </div>
    );
  }

  return (
    <div className="auth-form-card">
      <div className="auth-tablist">
        <span className="auth-tab auth-tab-active">Quên mật khẩu</span>
        <Link href="/login" className="auth-tab auth-tab-inactive">
          Đăng nhập
        </Link>
      </div>

      <div className="auth-header-block">
        <h1 className="auth-heading">Khôi phục tài khoản</h1>
        <p className="auth-subheading">
          Nhập email đã đăng ký, chúng tôi sẽ gửi liên kết để bạn đặt mật khẩu mới.
        </p>
      </div>

      <form className="auth-form-inner" onSubmit={handleSubmit} noValidate>
        <div className="auth-field-group">
          <label className="auth-field-label" htmlFor="fp-email">
            Email tài khoản
          </label>
          <div className="auth-input-wrapper">
            <svg className="auth-input-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
              <rect x="2" y="4" width="20" height="16" rx="2" />
              <path d="m22 7-8.97 5.7a1.94 1.94 0 0 1-2.06 0L2 7" />
            </svg>
            <input
              id="fp-email"
              type="email"
              autoComplete="email"
              className="auth-input-control auth-input-has-icon"
              value={email}
              disabled={isSubmitting}
              placeholder="ban@email.com"
              aria-invalid={Boolean(emailError)}
              aria-describedby={emailError ? "fp-email-error" : undefined}
              onChange={(e) => {
                setEmail(e.target.value);
                setEmailError(null);
              }}
              autoFocus
            />
          </div>
          {emailError && (
            <span id="fp-email-error" className="auth-field-error" role="alert">
              <span>{emailError}</span>
            </span>
          )}
        </div>

        {errorMessage && (
          <div className="auth-alert auth-alert-error" role="alert">
            <div className="auth-alert-content">
              <span>{errorMessage}</span>
            </div>
          </div>
        )}

        <button type="submit" className="auth-btn-primary" disabled={isSubmitting}>
          {isSubmitting ? (
            <>
              <span className="auth-spinner" aria-hidden="true" />
              <span>Đang gửi...</span>
            </>
          ) : (
            <span>Gửi liên kết đặt lại</span>
          )}
        </button>
      </form>

      <Link href="/login" className="auth-back-link">
        ← Quay lại đăng nhập
      </Link>
    </div>
  );
}
