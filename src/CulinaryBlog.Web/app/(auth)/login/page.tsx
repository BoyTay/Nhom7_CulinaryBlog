import type { Metadata } from "next";
import Link from "next/link";
import { Suspense } from "react";
import { LoginForm } from "@/components/auth/login-form";
import "../auth.css";

export const metadata: Metadata = {
  title: "Đăng nhập | Culinary Blog",
  description: "Đăng nhập vào tài khoản Culinary Blog của bạn.",
};

function LoginFormSkeleton() {
  return (
    <div className="auth-form-card" aria-busy="true" aria-label="Đang tải form đăng nhập">
      <div style={{ height: "44px", background: "#e9e7dd", borderRadius: "14px" }} />
      <div style={{ height: "38px", width: "60%", background: "#e9e7dd", borderRadius: "8px" }} />
      <div style={{ display: "grid", gap: "1rem", marginTop: "1rem" }}>
        <div style={{ height: "48px", background: "#f0efe9", borderRadius: "12px" }} />
        <div style={{ height: "48px", background: "#f0efe9", borderRadius: "12px" }} />
        <div style={{ height: "48px", background: "#2d5d43", opacity: 0.4, borderRadius: "12px" }} />
      </div>
    </div>
  );
}

export default function LoginPage() {
  return (
    <main className="ds-scope auth-split-container">
      {/* Left Hero Panel */}
      <section className="auth-hero-panel">
        <Link href="/" className="auth-hero-brand" aria-label="Culinary Blog - Trang chủ">
          Culinary Blog
        </Link>
        <div className="auth-hero-content">
          <h2 className="auth-hero-title">Vào bếp cùng cộng đồng.</h2>
          <p className="auth-hero-description">
            Đăng nhập để tạo công thức, thêm ảnh và xuất bản món ăn của bạn.
          </p>
        </div>
        <span className="auth-hero-footer">
          Phiên đăng nhập 15 phút, tự làm mới khi bạn còn hoạt động.
        </span>
      </section>

      {/* Right Form Panel */}
      <section className="auth-form-panel">
        <Suspense fallback={<LoginFormSkeleton />}>
          <LoginForm />
        </Suspense>
      </section>
    </main>
  );
}
