import type { Metadata } from "next";
import Link from "next/link";
import { Suspense } from "react";
import { LoginForm } from "@/components/auth/login-form";
import "../auth.css";

export const metadata: Metadata = {
  title: "Đăng nhập | Culinary Blog",
  description: "Đăng nhập vào tài khoản Culinary Blog của bạn để khám phá và chia sẻ công thức nấu ăn.",
};

function LoginFormSkeleton() {
  return (
    <div className="auth-form-card" aria-busy="true" aria-label="Đang tải form đăng nhập">
      <div style={{ height: "44px", background: "#ebe8de", borderRadius: "14px" }} />
      <div style={{ display: "flex", flexDirection: "column", gap: "6px" }}>
        <div style={{ height: "32px", width: "65%", background: "#ebe8de", borderRadius: "8px" }} />
        <div style={{ height: "18px", width: "85%", background: "#f0efe9", borderRadius: "6px" }} />
      </div>
      <div style={{ display: "grid", gap: "1rem", marginTop: "0.5rem" }}>
        <div style={{ height: "48px", background: "#f0efe9", borderRadius: "12px" }} />
        <div style={{ height: "48px", background: "#f0efe9", borderRadius: "12px" }} />
        <div style={{ height: "48px", background: "#2d5d43", opacity: 0.35, borderRadius: "12px" }} />
      </div>
    </div>
  );
}

export default function LoginPage() {
  return (
    <main className="ds-scope auth-split-container">
      {/* Left Hero Panel */}
      <section className="auth-hero-panel auth-hero-login">
        <div className="auth-hero-header auth-stagger-1">
          <span className="auth-hero-badge">
            <span className="auth-badge-dot" aria-hidden="true" />
            <span>Cộng đồng ẩm thực</span>
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
          <h2 className="auth-hero-title">Vào bếp cùng cộng đồng.</h2>
          <p className="auth-hero-description">
            Đăng nhập để lưu giữ bí quyết gia đình, tạo công thức sáng tạo và kết nối với những tâm hồn đồng điệu.
          </p>
          <div className="auth-hero-perks">
            <div className="auth-hero-perk-item">
              <svg className="auth-hero-perk-icon" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                <path fillRule="evenodd" d="M16.704 4.153a.75.75 0 01.143 1.052l-8 10.5a.75.75 0 01-1.127.075l-4.5-4.5a.75.75 0 011.06-1.06l3.894 3.893 7.48-9.817a.75.75 0 011.05-.143z" clipRule="evenodd" />
              </svg>
              <span>Hơn 3.500+ công thức được kiểm duyệt chi tiết</span>
            </div>
            <div className="auth-hero-perk-item">
              <svg className="auth-hero-perk-icon" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                <path fillRule="evenodd" d="M16.704 4.153a.75.75 0 01.143 1.052l-8 10.5a.75.75 0 01-1.127.075l-4.5-4.5a.75.75 0 011.06-1.06l3.894 3.893 7.48-9.817a.75.75 0 011.05-.143z" clipRule="evenodd" />
              </svg>
              <span>Định lượng nguyên liệu và từng bước nấu trực quan</span>
            </div>
            <div className="auth-hero-perk-item">
              <svg className="auth-hero-perk-icon" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                <path fillRule="evenodd" d="M16.704 4.153a.75.75 0 01.143 1.052l-8 10.5a.75.75 0 01-1.127.075l-4.5-4.5a.75.75 0 011.06-1.06l3.894 3.893 7.48-9.817a.75.75 0 011.05-.143z" clipRule="evenodd" />
              </svg>
              <span>Bảo mật an toàn phiên đăng nhập mã hóa chuẩn JWT</span>
            </div>
          </div>

          {/* Floating Recipe Highlight Card */}
          <div className="auth-floating-card">
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img
              src="https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=160&h=160&q=80"
              alt="Món ăn gợi ý"
              className="auth-floating-card-img"
              width={58}
              height={58}
              loading="lazy"
            />
            <div className="auth-floating-card-body">
              <span className="auth-floating-card-tag">Gợi ý hôm nay</span>
              <div className="auth-floating-card-title">Salad cá hồi sốt bơ thảo mộc</div>
              <div className="auth-floating-card-meta">
                <span className="auth-floating-card-rating">★ 4.9 (1.2k)</span>
                <span>•</span>
                <span>⏱️ 25 phút</span>
              </div>
            </div>
          </div>
        </div>

        <div className="auth-hero-footer auth-stagger-3">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
            <circle cx="12" cy="12" r="10" />
            <polyline points="12 6 12 12 16 14" />
          </svg>
          <span>Phiên làm việc an toàn, tự động làm mới khi bạn còn hoạt động.</span>
        </div>
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
