"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import type { ReactNode } from "react";
import { useAuth } from "@/lib/auth/use-auth";
import { canAccessAdmin } from "@/lib/navigation/access";
import { routes } from "@/lib/navigation/routes";
import { initials } from "@/lib/ui/format";
import "./dashboard.css";

export function DashboardShell({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const { user } = useAuth();
  const isAdmin = canAccessAdmin(user?.roles);
  const displayName = user?.displayName || user?.email || "";

  const links = [
    { label: "Tổng quan", href: routes.dashboard, exact: true, badge: false },
    { label: "Công thức của tôi", href: routes.dashboardRecipes, exact: false, badge: false },
    ...(isAdmin ? [{ label: "Danh mục", href: routes.dashboardCategories, exact: false, badge: true }] : []),
    { label: "Hồ sơ", href: routes.profile, exact: true, badge: false },
  ];

  return (
    <div className="dashboard">
      <aside className="dashboard-side">
        <Link href={routes.home} className="dashboard-brand">Culinary Blog</Link>
        <nav aria-label="Dashboard">
          {links.map((link) => {
            const current = link.exact ? pathname === link.href : pathname.startsWith(link.href);
            return (
              <Link key={link.href} href={link.href} aria-current={current ? "page" : undefined}>
                {link.label}
                {link.badge && <span className="dashboard-badge">Admin</span>}
              </Link>
            );
          })}
        </nav>
        {user && (
          <div className="dashboard-user">
            <span className="dashboard-user-avatar" aria-hidden="true">{initials(displayName)}</span>
            <div>
              <strong>{displayName}</strong>
              <span>{isAdmin ? "Quản trị viên" : "Tác giả"}</span>
            </div>
          </div>
        )}
      </aside>
      <main className="dashboard-main">{children}</main>
    </div>
  );
}
