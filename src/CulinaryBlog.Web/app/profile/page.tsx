import type { Metadata } from "next";
import Link from "next/link";
import { ProtectedRoute } from "@/components/auth/protected-route";
import { ProfileForm, SessionCard } from "@/components/auth/profile-form";
import { ChangePasswordForm } from "@/components/auth/change-password-form";
import { routes } from "@/lib/navigation/routes";
import "./profile.css";

export const metadata: Metadata = {
  title: "Hồ sơ cá nhân | Culinary Blog",
  description: "Quản lý thông tin tài khoản và hồ sơ người dùng trên Culinary Blog.",
};

function NavIcon({ path }: { path: string }) {
  return (
    <svg aria-hidden="true" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.9" strokeLinecap="round" strokeLinejoin="round">
      <path d={path} />
    </svg>
  );
}

export default function ProfilePage() {
  return (
    <ProtectedRoute>
      <div className="profile-page-wrapper">
        <main className="profile-main">
          <h1 className="profile-title">Hồ sơ cá nhân</h1>
          <div className="profile-layout">
            <nav aria-label="Tài khoản" className="profile-nav">
              <a href="#profile-info" className="side" aria-current="true"><NavIcon path="M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8zM4 21a8 8 0 0 1 16 0" />Thông tin cá nhân</a>
              <a href="#cp-title" className="side"><NavIcon path="M6 11h12v9H6zM9 11V8a3 3 0 0 1 6 0v3" />Mật khẩu & bảo mật</a>
              <Link href={routes.dashboardRecipes} className="side"><NavIcon path="M5 4h14v16H5zM9 9h6M9 13h6" />Công thức của tôi</Link>
            </nav>
            <div className="profile-content">
              <ProfileForm />
              <ChangePasswordForm />
              <SessionCard />
            </div>
          </div>
        </main>
      </div>
    </ProtectedRoute>
  );
}
