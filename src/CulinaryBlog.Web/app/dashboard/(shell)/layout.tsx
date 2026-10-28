import type { ReactNode } from "react";
import { ProtectedRoute } from "@/components/auth/protected-route";
import { DashboardShell } from "@/components/dashboard/dashboard-shell";
import { RECIPE_EDITOR_ROLES } from "@/lib/recipes/editor";

export default function DashboardShellLayout({ children }: { children: ReactNode }) {
  return (
    <ProtectedRoute requiredRoles={[...RECIPE_EDITOR_ROLES]}>
      <DashboardShell>{children}</DashboardShell>
    </ProtectedRoute>
  );
}
