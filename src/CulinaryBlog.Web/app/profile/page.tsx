import type { Metadata } from "next";
import { ProtectedRoute } from "@/components/auth/protected-route";
import { ProfileForm } from "@/components/auth/profile-form";
import { SiteHeader } from "@/components/shared/site-header";
import "./profile.css";

export const metadata: Metadata = {
  title: "Hồ sơ cá nhân | Culinary Blog",
  description: "Quản lý thông tin tài khoản và hồ sơ người dùng trên Culinary Blog.",
};

export default function ProfilePage() {
  return (
    <ProtectedRoute>
      <div className="ds-scope profile-page-wrapper">
        <SiteHeader />
        <main className="profile-main">
          <h1 className="profile-title">Hồ sơ cá nhân</h1>
          <ProfileForm />
        </main>
      </div>
    </ProtectedRoute>
  );
}

