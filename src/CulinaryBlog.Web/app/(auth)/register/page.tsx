import type { Metadata } from "next";
import Link from "next/link";
import { Suspense } from "react";
import { RegisterForm } from "@/components/auth/register-form";
import "../auth.css";

export const metadata: Metadata = {
  title: "Đăng ký",
  description: "Tạo tài khoản mới tại Culinary Blog.",
};

function RegisterFormSkeleton() {
  return (
    <div className="auth-card" aria-busy="true" aria-label="Đang tải form đăng ký">
      <div className="auth-card-header">
        <h1 className="auth-title">Đăng ký tài khoản</h1>
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

export default function RegisterPage() {
  return (
    <main className="auth-wrapper">
      <Link href="/" className="auth-brand" aria-label="Culinary Blog - Trang chủ">
        <span className="auth-brand-mark" aria-hidden="true">
          C
        </span>
        <span>Culinary Blog</span>
      </Link>
      <Suspense fallback={<RegisterFormSkeleton />}>
        <RegisterForm />
      </Suspense>
      <footer className="auth-page-footer">
        <p>Nhóm 7 · Culinary Blog · 2026–2027</p>
      </footer>
    </main>
  );
}

