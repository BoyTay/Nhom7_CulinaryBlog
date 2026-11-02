"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, useRef, useState } from "react";
import { useAuth } from "@/lib/auth/use-auth";
import { buildNavItems, isActiveNav, routes } from "@/lib/navigation/routes";
import { AccountMenu, MenuIcon } from "./account-menu";
import { BrandLogo } from "./brand-logo";
import { MobileMenu } from "./mobile-menu";
import "./site-header.css";

export function SiteHeader() {
  const { user, isAuthenticated, isLoading, logout } = useAuth();
  const pathname = usePathname();
  const router = useRouter();
  const [menuOpen, setMenuOpen] = useState(false);
  const menuButtonRef = useRef<HTMLButtonElement>(null);

  const navItems = buildNavItems(user ? { roles: user.roles ?? [] } : null);

  useEffect(() => {
    setMenuOpen(false);
  }, [pathname]);

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
    <AccountMenu />
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
