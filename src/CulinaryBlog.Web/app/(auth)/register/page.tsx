import type { Metadata } from "next";
import { Suspense } from "react";
import { AuthPhotoShell } from "@/components/auth/auth-shell";
import { RegisterForm } from "@/components/auth/register-form";
import { Skeleton } from "@/components/shared";
import "../auth.css";

export const metadata: Metadata = {
  title: "Đăng ký tài khoản | Culinary Blog",
  description: "Tạo tài khoản Culinary Blog để chia sẻ công thức nấu ăn của riêng bạn.",
};

export default function RegisterPage() {
  return (
    <AuthPhotoShell
      photo="/images/recipes/comtam.jpg"
      stackPhotos={["/images/recipes/pho.jpg", "/images/recipes/pho2.jpg", "/images/recipes/che.jpg"]}
      blurb="Tạo tài khoản để lưu bản nháp, đăng công thức của riêng bạn và xây dựng hồ sơ tác giả của mình."
      photoCredit="Ảnh nền: Phương Huy / Wikimedia Commons (CC0)"
    >
      <Suspense fallback={<div className="auth-form-card" aria-busy="true" aria-label="Đang tải form đăng ký"><Skeleton height="520px" /></div>}>
        <RegisterForm />
      </Suspense>
    </AuthPhotoShell>
  );
}
