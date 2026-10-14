import type { Metadata } from "next";
import Link from "next/link";
import { Suspense } from "react";
import { LoginForm } from "@/components/auth/login-form";
import "../auth.css";

export const metadata: Metadata = {
  title: "Đăng nhập",
  description: "Đăng nhập vào tài khoản Culinary Blog của bạn.",
};

function LoginFormSkeleton() {
  return (
    <div className="auth-card" aria-busy="true" aria-label="Đang tải form đăng nhập">
      <div className="auth-card-header">
        <h1 className="auth-title">Đăng nhập</h1>
        <p className="auth-subtitle">Đang nạp dữ liệu...</p>
      </div>
      <div style={{ display: "grid", gap: "1rem", opacity: 0.6 }}>
        <div
          style={{
            height: "42px",
            background: "var(--color-muted)",
            borderRadius: "var(--radius-sm)",
          }}
        />
        <div
          style={{
            height: "42px",
            background: "var(--color-muted)",
            borderRadius: "var(--radius-sm)",
          }}
        />
        <div
          style={{
            height: "48px",
            background: "var(--color-muted)",
            borderRadius: "var(--radius-sm)",
          }}
        />
      </div>
    </div>
  );
}

export default function LoginPage() {
  return (
    <main className="auth-wrapper">
      <Link href="/" className="auth-brand" aria-label="Culinary Blog - Trang chủ">
        <span className="auth-brand-mark" aria-hidden="true">
          C
        </span>
        <span>Culinary Blog</span>
      </Link>
      <Suspense fallback={<LoginFormSkeleton />}>
        <LoginForm />
      </Suspense>
      <footer className="auth-page-footer">
        <p>Nhóm 7 · Culinary Blog · 2026–2027</p>
      </footer>
    </main>
  );
}

