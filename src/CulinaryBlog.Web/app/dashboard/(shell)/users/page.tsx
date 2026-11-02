import type { Metadata } from "next";
import { ProtectedRoute } from "@/components/auth/protected-route";
import UserManager from "./user-manager";

export const metadata: Metadata = {
  title: "Quản lý tài khoản",
  robots: { index: false, follow: false },
};

export default function DashboardUsersPage() {
  return (
    <ProtectedRoute requiredRoles={["Admin"]}>
      <UserManager />
    </ProtectedRoute>
  );
}
