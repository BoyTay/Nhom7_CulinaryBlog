import type { Metadata } from "next";
import { Suspense } from "react";
import { AuthRecoveryShell } from "@/components/auth/auth-shell";
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
    <AuthRecoveryShell>
      <Suspense fallback={<div className="auth-form-card">Đang tải form...</div>}>
        <ResetPasswordForm />
      </Suspense>
    </AuthRecoveryShell>
  );
}
