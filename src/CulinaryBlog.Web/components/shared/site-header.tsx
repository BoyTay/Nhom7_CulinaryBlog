"use client";

import Image from "next/image";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, useRef, useState } from "react";
import { useAuth } from "@/lib/auth/use-auth";
import { canAccessAdmin } from "@/lib/navigation/access";
import { buildNavItems, isActiveNav, routes } from "@/lib/navigation/routes";
import { initials } from "@/lib/ui/format";
import { BrandLogo } from "./brand-logo";
import { MobileMenu } from "./mobile-menu";
import "./site-header.css";

function MenuIcon({ path }: { path: string }) {
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

export function SiteHeader() {
  const { user, isAuthenticated, isLoading, logout } = useAuth();
  const pathname = usePathname();
  const router = useRouter();
  const [menuOpen, setMenuOpen] = useState(false);
  const menuButtonRef = useRef<HTMLButtonElement>(null);
  const userMenuRef = useRef<HTMLDetailsElement>(null);

  const navItems = buildNavItems(user ? { roles: user.roles ?? [] } : null);
  const displayName = user?.displayName || user?.email || "";
  const isAdmin = canAccessAdmin(user?.roles);
  const roleLabel = isAdmin ? "Admin" : "Tác giả";

  useEffect(() => {
    setMenuOpen(false);
    userMenuRef.current?.removeAttribute("open");
  }, [pathname]);

  useEffect(() => {
    function closeUserMenu(event: MouseEvent | KeyboardEvent) {
      const menu = userMenuRef.current;
      if (!menu?.open) return;
      if (event instanceof KeyboardEvent ? event.key === "Escape" : !menu.contains(event.target as Node)) {
        menu.removeAttribute("open");
      }
    }
    document.addEventListener("click", closeUserMenu);
    document.addEventListener("keydown", closeUserMenu);
    return () => {
      document.removeEventListener("click", closeUserMenu);
      document.removeEventListener("keydown", closeUserMenu);
    };
  }, []);

  function closeMenu() {
    setMenuOpen(false);
    menuButtonRef.current?.focus();
  }

  async function signOut() {
    await logout();
    router.replace(routes.home);
  }

  const account = isLoading ? (
    <span className="hd-skeleton" aria-hidden="true" />
  ) : isAuthenticated && user ? (
    <>
    <Link href={routes.dashboardRecipeNew} className="hb hb-p">Viết công thức</Link>
    <details className="hd-user hd-detail" ref={userMenuRef}>
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
            <span className={isAdmin ? "role adm" : "role"}>{roleLabel}</span>
          </div>
        </div>
        <hr />
        <Link href={routes.profile} className="mi"><MenuIcon path="M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8zM4 21a8 8 0 0 1 16 0" />Hồ sơ cá nhân</Link>
        <Link href={routes.dashboardRecipes} className="mi"><MenuIcon path="M5 4h14v16H5zM9 9h6M9 13h6" />Công thức của tôi</Link>
        <Link href={routes.dashboardRecipeNew} className="mi"><MenuIcon path="M12 5v14M5 12h14" />Viết công thức</Link>
        {isAdmin && (
          <Link href={routes.dashboardCategories} className="mi"><MenuIcon path="M4 4h7v7H4zM13 4h7v7h-7zM4 13h7v7H4z" />Quản lý danh mục <span className="role adm mi-badge">Admin</span></Link>
        )}
        <hr />
        <button type="button" className="mi out" onClick={signOut}><MenuIcon path="M10 4H5v16h5M15 8l4 4-4 4M19 12H9" />Đăng xuất</button>
      </div>
    </details>
    </>
  ) : (
    <>
      <Link href={routes.login} className="hb hb-g">Đăng nhập</Link>
      <Link href={routes.register} className="hb hb-p">Đăng ký</Link>
    </>
  );

  return (
    <header className="hd">
      <div className="hd-in">
        <Link href={routes.home} className="hd-logo"><BrandLogo size={46} />Culinary Blog</Link>

        <nav aria-label="Chính" className="hd-nav">
          {navItems.map((item) => {
            const active = isActiveNav(pathname, item.href);
            return (
              <Link key={item.href} href={item.href} className={active ? "hd-l on" : "hd-l"} aria-current={active ? "page" : undefined}>
                {item.label}
              </Link>
            );
          })}
        </nav>

        <div className="hd-auth">{account}</div>
        {!isLoading && (
          <Link
            href={isAuthenticated ? routes.profile : routes.login}
            className="hd-account"
            aria-label={isAuthenticated ? "Hồ sơ cá nhân" : "Đăng nhập"}
          >
            <svg aria-hidden="true" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round"><circle cx="12" cy="8" r="4" /><path d="M4 21a8 8 0 0 1 16 0" /></svg>
          </Link>
        )}

        <button
          ref={menuButtonRef}
          type="button"
          className="hd-burger"
          aria-label={menuOpen ? "Đóng menu" : "Mở menu"}
          aria-expanded={menuOpen}
          aria-controls="mobile-menu"
          onClick={() => setMenuOpen((open) => !open)}
        >
          <svg aria-hidden="true" width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round"><path d="M4 7h16M4 12h16M4 17h16" /></svg>
        </button>

        <MobileMenu open={menuOpen} onClose={closeMenu}>
          <nav aria-label="Menu chính">
            {navItems.map((item) => {
              const active = isActiveNav(pathname, item.href);
              return (
                <Link key={item.href} href={item.href} className={active ? "hd-l on" : "hd-l"} aria-current={active ? "page" : undefined}>
                  {item.label}
                </Link>
              );
            })}
          </nav>
          <hr />
          {!isLoading && isAuthenticated && user ? (
            <>
              <Link href={routes.profile} className="mi"><MenuIcon path="M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8zM4 21a8 8 0 0 1 16 0" />Hồ sơ cá nhân</Link>
              <Link href={routes.dashboardRecipes} className="mi"><MenuIcon path="M5 4h14v16H5zM9 9h6M9 13h6" />Công thức của tôi</Link>
              <button type="button" className="mi out" onClick={signOut}><MenuIcon path="M10 4H5v16h5M15 8l4 4-4 4M19 12H9" />Đăng xuất</button>
            </>
          ) : !isLoading ? (
            <>
              <Link href={routes.login} className="hb hb-g mobile-menu-start">Đăng nhập</Link>
              <Link href={routes.register} className="hb hb-p">Đăng ký</Link>
            </>
          ) : null}
        </MobileMenu>
      </div>
    </header>
  );
}
