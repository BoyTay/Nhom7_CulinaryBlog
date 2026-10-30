import type { Metadata } from "next";
import { AuthRecoveryShell } from "@/components/auth/auth-shell";
import { ForgotPasswordForm } from "@/components/auth/forgot-password-form";
import "../auth.css";

export const metadata: Metadata = {
  title: "Quên mật khẩu | Culinary Blog",
  description: "Khôi phục và đặt lại mật khẩu tài khoản Culinary Blog của bạn.",
};

export default function ForgotPasswordPage() {
  return (
    <AuthRecoveryShell>
      <ForgotPasswordForm />
    </AuthRecoveryShell>
  );
}
