import type { Metadata } from "next";
import { ProtectedRoute } from "@/components/auth/protected-route";
import CategoryManager from "./category-manager";

export const metadata: Metadata = {
  title: "Quản lý danh mục",
  robots: { index: false, follow: false },
};

export default function DashboardCategoriesPage() {
  return (
    <ProtectedRoute requiredRoles={["Admin"]}>
      <CategoryManager />
    </ProtectedRoute>
  );
}
