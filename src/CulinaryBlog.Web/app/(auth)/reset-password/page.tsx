import type { Metadata } from "next";
import Link from "next/link";
import { Suspense } from "react";
import { ResetPasswordForm } from "@/components/auth/reset-password-form";
import "../auth.css";

export const metadata: Metadata = {
  title: "Đặt lại mật khẩu | Culinary Blog",
  description: "Đặt mật khẩu mới cho tài khoản Culinary Blog bằng liên kết trong email.",
  // The reset token travels in the query string; never leak it through the Referer header.
  referrer: "no-referrer",
  robots: { index: false, follow: false },
};

export default function ResetPasswordPage() {
  return (
    <main className="auth-split-container">
      {/* Left Hero Panel */}
      <section className="auth-hero-panel auth-hero-login">
        <div className="auth-hero-header auth-stagger-1">
          <span className="auth-hero-badge">
            <span className="auth-badge-dot" aria-hidden="true" />
            <span>Bảo mật tài khoản</span>
          </span>
          <Link href="/" className="auth-hero-brand" aria-label="Culinary Blog - Về trang chủ">
            <span className="auth-hero-logo-icon" aria-hidden="true">
              <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <path d="M6 13.87A4 4 0 0 1 7.41 6a5.11 5.11 0 0 1 1.05-1.54 5 5 0 0 1 7.08 0A5.11 5.11 0 0 1 16.59 6 4 4 0 0 1 18 13.87V21H6Z" />
                <line x1="6" y1="17" x2="18" y2="17" />
              </svg>
            </span>
            <span>Culinary Blog</span>
          </Link>
        </div>

        <div className="auth-hero-content auth-stagger-2">
          <h2 className="auth-hero-title">Một mật khẩu mới, một khởi đầu an toàn.</h2>
          <p className="auth-hero-description">
            Liên kết trong email chỉ dùng được một lần và hết hạn sau 30 phút. Sau khi đặt lại, mọi phiên đăng nhập cũ đều bị thu hồi.
          </p>
          <div className="auth-hero-perks">
            <div className="auth-hero-perk-item">
              <svg className="auth-hero-perk-icon" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                <path fillRule="evenodd" d="M16.704 4.153a.75.75 0 01.143 1.052l-8 10.5a.75.75 0 01-1.127.075l-4.5-4.5a.75.75 0 011.06-1.06l3.894 3.893 7.48-9.817a.75.75 0 011.05-.143z" clipRule="evenodd" />
              </svg>
              <span>Liên kết dùng một lần, hết hạn sau 30 phút</span>
            </div>
            <div className="auth-hero-perk-item">
              <svg className="auth-hero-perk-icon" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                <path fillRule="evenodd" d="M16.704 4.153a.75.75 0 01.143 1.052l-8 10.5a.75.75 0 01-1.127.075l-4.5-4.5a.75.75 0 011.06-1.06l3.894 3.893 7.48-9.817a.75.75 0 011.05-.143z" clipRule="evenodd" />
              </svg>
              <span>Mã hóa bảo vệ mật khẩu an toàn chuẩn PBKDF2</span>
            </div>
          </div>
        </div>

        <div className="auth-hero-footer auth-stagger-3">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
            <rect x="3" y="11" width="18" height="11" rx="2" ry="2" />
            <path d="M7 11V7a5 5 0 0 1 10 0v4" />
          </svg>
          <span>Quy trình bảo mật tài khoản chuẩn công nghệ cao.</span>
        </div>
      </section>

      {/* Right Form Panel */}
      <section className="auth-form-panel">
        <Suspense fallback={<div>Đang tải form...</div>}>
          <ResetPasswordForm />
        </Suspense>
      </section>
    </main>
  );
}
