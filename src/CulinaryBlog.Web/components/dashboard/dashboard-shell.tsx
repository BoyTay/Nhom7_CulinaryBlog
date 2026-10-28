"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import type { ReactNode } from "react";
import { useAuth } from "@/lib/auth/use-auth";
import { canAccessAdmin } from "@/lib/navigation/access";
import { routes } from "@/lib/navigation/routes";
import "./dashboard.css";

export function DashboardShell({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const { user } = useAuth();
  const isAdmin = canAccessAdmin(user?.roles);

  const links = [
    { label: "Tổng quan", href: routes.dashboard, exact: true },
    { label: "Công thức của tôi", href: routes.dashboardRecipes, exact: false },
    ...(isAdmin ? [{ label: "Danh mục", href: routes.dashboardCategories, exact: false }] : []),
  ];

  return (
    <div className="dashboard ds-scope">
      <aside className="dashboard-side">
        <nav aria-label="Bảng điều khiển">
          {links.map((link) => {
            const current = link.exact ? pathname === link.href : pathname.startsWith(link.href);
            return (
              <Link key={link.href} href={link.href} aria-current={current ? "page" : undefined}>
                {link.label}
              </Link>
            );
          })}
        </nav>
        <Link className="button button-primary dashboard-new" href={routes.dashboardRecipeNew}>
          Viết công thức mới
        </Link>
      </aside>
      <main className="dashboard-main">{children}</main>
    </div>
  );
}
