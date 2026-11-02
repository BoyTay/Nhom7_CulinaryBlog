"use client";

import Image from "next/image";
import Link from "next/link";
import { usePathname } from "next/navigation";
import type { ReactNode } from "react";
import { AccountMenu } from "@/components/shared/account-menu";
import { BrandLogo } from "@/components/shared/brand-logo";
import { useAuth } from "@/lib/auth/use-auth";
import { canAccessAdmin } from "@/lib/navigation/access";
import { routes } from "@/lib/navigation/routes";
import { initials } from "@/lib/ui/format";
import "./dashboard.css";

const icons = {
  overview: "M4 11l8-7 8 7v9H4zM10 20v-6h4v6",
  recipes: "M5 4h14v16H5zM9 9h6M9 13h6",
  write: "M12 5v14M5 12h14",
  profile: "M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8zM4 21a8 8 0 0 1 16 0",
  categories: "M4 4h7v7H4zM13 4h7v7h-7zM4 13h7v7H4zM13 13h7v7h-7z",
  users: "M9 4.5a3.5 3.5 0 1 0 0 7 3.5 3.5 0 0 0 0-7zM2.5 20a6.5 6.5 0 0 1 13 0M16 4.5a3.5 3.5 0 0 1 0 7M18 14.5a6.5 6.5 0 0 1 3.5 5.5",
};

function SideLink({ href, label, icon, current }: { href: string; label: string; icon: string; current: boolean }) {
  return (
    <Link href={href} className="side" aria-current={current ? "page" : undefined}>
      <svg aria-hidden="true" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.9" strokeLinecap="round" strokeLinejoin="round"><path d={icon} /></svg>
      {label}
    </Link>
  );
}

export function DashboardShell({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const { user } = useAuth();
  const isAdmin = canAccessAdmin(user?.roles);
  const displayName = user?.displayName || user?.email || "";
  const onAdminPage = isAdmin && (pathname.startsWith(routes.dashboardCategories) || pathname.startsWith(routes.dashboardUsers));

  return (
    <div className="dashboard">
      <aside className="sb dashboard-side">
        <Link href={routes.home} className="dashboard-brand" aria-label="Culinary Blog – trang chủ"><BrandLogo size={46} />Culinary Blog</Link>
        <nav aria-label={onAdminPage ? "Khu vực quản trị" : "Khu vực tác giả"}>
          <SideLink href={routes.dashboard} label="Tổng quan" icon={icons.overview} current={pathname === routes.dashboard} />
          <SideLink href={routes.dashboardRecipes} label="Công thức của tôi" icon={icons.recipes} current={pathname === routes.dashboardRecipes} />
          <SideLink href={routes.dashboardRecipeNew} label="Viết công thức mới" icon={icons.write} current={false} />
          <SideLink href={routes.profile} label="Hồ sơ cá nhân" icon={icons.profile} current={false} />
          {isAdmin && (
            <>
              <span className="dashboard-side-label">Quản trị</span>
              <SideLink href={routes.dashboardCategories} label="Danh mục" icon={icons.categories} current={pathname.startsWith(routes.dashboardCategories)} />
              <SideLink href={routes.dashboardUsers} label="Tài khoản" icon={icons.users} current={pathname.startsWith(routes.dashboardUsers)} />
            </>
          )}
        </nav>
        {user && (
          <div className="dashboard-user">
            <span className={isAdmin ? "dashboard-user-avatar is-admin" : "dashboard-user-avatar"} aria-hidden="true">
              {user.avatarUrl ? <Image src={user.avatarUrl} alt="" width={40} height={40} unoptimized /> : initials(displayName)}
            </span>
            <div>
              <strong>{displayName}</strong>
              <span>{isAdmin ? "Admin" : "Tác giả"}</span>
            </div>
          </div>
        )}
      </aside>
      <main className="dashboard-main">
        <div className="dashboard-topbar">
          {onAdminPage && <span className="dashboard-admin-chip">Chế độ quản trị</span>}
          <AccountMenu />
        </div>
        {children}
      </main>
    </div>
  );
}
