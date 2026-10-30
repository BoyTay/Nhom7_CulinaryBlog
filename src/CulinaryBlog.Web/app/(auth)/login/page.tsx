import type { Metadata } from "next";
import { Suspense } from "react";
import { AuthPhotoShell } from "@/components/auth/auth-shell";
import { LoginForm } from "@/components/auth/login-form";
import { Skeleton } from "@/components/shared";
import "../auth.css";

export const metadata: Metadata = {
  title: "Đăng nhập | Culinary Blog",
  description: "Đăng nhập vào tài khoản Culinary Blog của bạn để khám phá và chia sẻ công thức nấu ăn.",
};

export default function LoginPage() {
  return (
    <AuthPhotoShell
      photo="/images/recipes/pho.jpg"
      stackPhotos={["/images/recipes/comtam.jpg", "/images/recipes/bunbo.jpg", "/images/recipes/che.jpg"]}
      blurb="Đăng nhập để lưu bản nháp, đăng công thức của riêng bạn và quản lý hồ sơ tác giả."
      photoCredit="Ảnh nền: Andy Li / Wikimedia Commons (CC0)"
    >
      <Suspense fallback={<div className="auth-form-card" aria-busy="true" aria-label="Đang tải form đăng nhập"><Skeleton height="360px" /></div>}>
        <LoginForm />
      </Suspense>
    </AuthPhotoShell>
  );
}
