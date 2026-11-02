"use client";

import Image from "next/image";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, useRef } from "react";
import { useAuth } from "@/lib/auth/use-auth";
import { canAccessAdmin } from "@/lib/navigation/access";
import { routes } from "@/lib/navigation/routes";
import { initials } from "@/lib/ui/format";
import "./site-header.css";

export function MenuIcon({ path }: { path: string }) {
  return (
    <svg aria-hidden="true" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.9" strokeLinecap="round" strokeLinejoin="round">
      <path d={path} />
    </svg>
  );
}

function UserAvatar({ name, avatarUrl, admin, size = 36 }: { name: string; avatarUrl?: string | null; admin: boolean; size?: number }) {
  return (
    <span className={admin ? "hd-av adm" : "hd-av"} style={{ width: size, height: size }}>
      {avatarUrl ? <Image src={avatarUrl} alt="" width={size} height={size} unoptimized /> : initials(name)}
    </span>
  );
}

/** The signed-in user's dropdown. Renders nothing for guests, so callers decide what guests see. */
export function AccountMenu() {
  const { user, logout } = useAuth();
  const pathname = usePathname();
  const router = useRouter();
  const menuRef = useRef<HTMLDetailsElement>(null);

  useEffect(() => {
    menuRef.current?.removeAttribute("open");
  }, [pathname]);

  useEffect(() => {
    function closeMenu(event: MouseEvent | KeyboardEvent) {
      const menu = menuRef.current;
      if (!menu?.open) return;
      if (event instanceof KeyboardEvent ? event.key === "Escape" : !menu.contains(event.target as Node)) {
        menu.removeAttribute("open");
      }
    }
    document.addEventListener("click", closeMenu);
    document.addEventListener("keydown", closeMenu);
    return () => {
      document.removeEventListener("click", closeMenu);
      document.removeEventListener("keydown", closeMenu);
    };
  }, []);

  async function signOut() {
    await logout();
    router.replace(routes.home);
  }

  if (!user) return null;

  const displayName = user.displayName || user.email;
  const isAdmin = canAccessAdmin(user.roles);

  return (
    <details className="hd-user hd-detail" ref={menuRef}>
      <summary aria-label={`Menu tài khoản của ${displayName}`}>
        <UserAvatar name={displayName} avatarUrl={user.avatarUrl} admin={isAdmin} />
        <span className="hd-user-name">{displayName}</span>
        <svg aria-hidden="true" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round"><path d="M6 9l6 6 6-6" /></svg>
      </summary>
      <div className="hd-menu">
        <div className="who">
          <UserAvatar name={displayName} avatarUrl={user.avatarUrl} admin={isAdmin} size={44} />
          <div>
            <strong>{displayName}</strong>
            <span className="m">{user.email}</span>
            <span className={isAdmin ? "role adm" : "role"}>{isAdmin ? "Admin" : "Tác giả"}</span>
          </div>
        </div>
        <hr />
        <Link href={routes.profile} className="mi"><MenuIcon path="M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8zM4 21a8 8 0 0 1 16 0" />Hồ sơ cá nhân</Link>
        <Link href={routes.dashboardRecipes} className="mi"><MenuIcon path="M5 4h14v16H5zM9 9h6M9 13h6" />Công thức của tôi</Link>
        <Link href={routes.dashboardRecipeNew} className="mi"><MenuIcon path="M12 5v14M5 12h14" />Viết công thức</Link>
        {isAdmin && (
          <>
            <Link href={routes.dashboardCategories} className="mi"><MenuIcon path="M4 4h7v7H4zM13 4h7v7h-7zM4 13h7v7H4z" />Quản lý danh mục <span className="role adm mi-badge">Admin</span></Link>
            <Link href={routes.dashboardUsers} className="mi"><MenuIcon path="M9 4.5a3.5 3.5 0 1 0 0 7 3.5 3.5 0 0 0 0-7zM2.5 20a6.5 6.5 0 0 1 13 0" />Quản lý tài khoản <span className="role adm mi-badge">Admin</span></Link>
          </>
        )}
        <hr />
        <button type="button" className="mi out" onClick={signOut}><MenuIcon path="M10 4H5v16h5M15 8l4 4-4 4M19 12H9" />Đăng xuất</button>
      </div>
    </details>
  );
}
