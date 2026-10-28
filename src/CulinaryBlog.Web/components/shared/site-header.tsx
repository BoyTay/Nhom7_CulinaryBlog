"use client";

import Image from "next/image";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, useRef, useState, type FormEvent } from "react";
import { useAuth } from "@/lib/auth/use-auth";
import { buildSearchHref } from "@/lib/navigation/search-href";
import { buildNavItems, isActiveNav, routes } from "@/lib/navigation/routes";
import { initials } from "@/lib/ui/format";
import { MobileMenu } from "./mobile-menu";
import "./site-header.css";

function SearchIcon({ size = 18 }: { size?: number }) {
  return (
    <svg aria-hidden="true" width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
      <circle cx="11" cy="11" r="7" />
      <path d="M20 20l-3.5-3.5" />
    </svg>
  );
}

export function SiteHeader() {
  const { user, isAuthenticated, isLoading } = useAuth();
  const pathname = usePathname();
  const router = useRouter();
  const [menuOpen, setMenuOpen] = useState(false);
  const [query, setQuery] = useState("");
  const menuButtonRef = useRef<HTMLButtonElement>(null);

  const navItems = buildNavItems(user ? { roles: user.roles ?? [] } : null);
  const displayName = user?.displayName || user?.email || "";

  useEffect(() => {
    setMenuOpen(false);
  }, [pathname]);

  function submitSearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    router.push(buildSearchHref(query));
    setQuery("");
    setMenuOpen(false);
  }

  function closeMenu() {
    setMenuOpen(false);
    menuButtonRef.current?.focus();
  }

  const account = isLoading ? (
    <span className="site-header-skeleton" aria-hidden="true" />
  ) : isAuthenticated && user ? (
    <Link
      href={routes.profile}
      className="site-header-avatar"
      aria-label={`Hồ sơ cá nhân của ${displayName}`}
      title={displayName}
    >
      {user.avatarUrl ? (
        <Image src={user.avatarUrl} alt="" width={40} height={40} unoptimized />
      ) : (
        <span>{initials(displayName)}</span>
      )}
    </Link>
  ) : (
    <>
      <Link href={routes.login} className="btn btn-ghost">Đăng nhập</Link>
      <Link href={routes.register} className="btn btn-primary">Đăng ký</Link>
    </>
  );

  return (
    <header className="site-header-bar">
      <div className="site-header-inner">
        <Link href={routes.home} className="site-header-brand">Culinary Blog</Link>

        <nav aria-label="Chính" className="site-header-nav">
          {navItems.map((item) => (
            <Link
              key={item.href}
              href={item.href}
              aria-current={isActiveNav(pathname, item.href) ? "page" : undefined}
            >
              {item.label}
            </Link>
          ))}
        </nav>

        <form role="search" className="site-header-search" onSubmit={submitSearch} action={routes.search}>
          <SearchIcon />
          <label htmlFor="site-search" className="visually-hidden">Tìm công thức</label>
          <input
            id="site-search"
            name="q"
            type="search"
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder="Tìm công thức, nguyên liệu…"
            autoComplete="off"
          />
        </form>

        <div className="site-header-actions">{account}</div>

        <div className="site-header-mobile">
          <Link href={routes.search} className="site-header-icon" aria-label="Tìm kiếm"><SearchIcon size={22} /></Link>
          <button
            ref={menuButtonRef}
            type="button"
            className="site-header-icon"
            aria-label={menuOpen ? "Đóng menu" : "Mở menu"}
            aria-expanded={menuOpen}
            aria-controls="mobile-menu"
            onClick={() => setMenuOpen((open) => !open)}
          >
            <svg aria-hidden="true" width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <path d="M4 7h16M4 12h16M4 17h16" />
            </svg>
          </button>
        </div>
      </div>

      <MobileMenu open={menuOpen} onClose={closeMenu}>
        <nav aria-label="Menu chính">
          {navItems.map((item) => (
            <Link
              key={item.href}
              href={item.href}
              aria-current={isActiveNav(pathname, item.href) ? "page" : undefined}
            >
              {item.label}
            </Link>
          ))}
        </nav>
        <form role="search" className="mobile-menu-search" onSubmit={submitSearch} action={routes.search}>
          <label htmlFor="mobile-search" className="visually-hidden">Tìm công thức</label>
          <input
            id="mobile-search"
            name="q"
            type="search"
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder="Tìm công thức, nguyên liệu…"
            autoComplete="off"
          />
          <button type="submit" className="btn btn-primary btn-square">Tìm</button>
        </form>
        {!isLoading && !isAuthenticated && (
          <div className="mobile-menu-actions">
            <Link href={routes.login} className="btn btn-outline">Đăng nhập</Link>
            <Link href={routes.register} className="btn btn-primary">Đăng ký</Link>
          </div>
        )}
      </MobileMenu>
    </header>
  );
}
