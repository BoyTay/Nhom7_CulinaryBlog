"use client";

import Image from "next/image";
import Link from "next/link";
import { useAuth } from "@/lib/auth/use-auth";

function getInitials(name?: string, email?: string): string {
  if (name && name.trim()) {
    const parts = name.trim().split(/\s+/);
    if (parts.length >= 2) {
      return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
    }
    return parts[0].slice(0, 2).toUpperCase();
  }
  if (email && email.trim()) {
    return email.trim().slice(0, 2).toUpperCase();
  }
  return "CB";
}

export function SiteHeader() {
  const { user, isAuthenticated, isLoading } = useAuth();
  const initials = getInitials(user?.displayName, user?.email);

  const isAuthorOrAdmin =
    user?.roles?.some((r) =>
      ["admin", "administrator", "chef", "author", "creator"].includes(r.toLowerCase()),
    ) ?? false;

  return (
    <header
      style={{
        background: "#ffffff",
        borderBottom: "1px solid #e2e0d6",
        position: "sticky",
        top: 0,
        zIndex: 40,
      }}
    >
      <div
        style={{
          maxWidth: "1200px",
          margin: "0 auto",
          padding: "0 24px",
          minHeight: "72px",
          display: "flex",
          flexWrap: "wrap",
          alignItems: "center",
          gap: "16px 32px",
        }}
      >
        <Link
          href="/"
          style={{
            fontFamily: "'Bricolage Grotesque', system-ui, sans-serif",
            fontWeight: 700,
            fontSize: "22px",
            color: "#2d5d43",
            textDecoration: "none",
          }}
        >
          Culinary Blog
        </Link>

        <nav
          aria-label="Chính"
          style={{
            display: "flex",
            gap: "24px",
            fontWeight: 500,
            fontSize: "15px",
            flex: 1,
            alignItems: "center",
          }}
        >
          <Link
            href="/recipes"
            style={{ color: "#1c2118", textDecoration: "none" }}
          >
            Công thức
          </Link>
          <Link
            href="/categories"
            style={{ color: "#1c2118", textDecoration: "none" }}
          >
            Danh mục
          </Link>
          <Link
            href="/search"
            style={{ color: "#1c2118", textDecoration: "none" }}
          >
            Tìm kiếm
          </Link>
          {isAuthorOrAdmin && (
            <Link
              href="/admin/categories"
              style={{ color: "#1c2118", textDecoration: "none" }}
            >
              Dashboard
            </Link>
          )}
        </nav>

        <div style={{ display: "flex", alignItems: "center", gap: "12px" }}>
          {isLoading ? (
            <div
              style={{
                width: "40px",
                height: "40px",
                borderRadius: "50%",
                background: "#e4eee7",
                animation: "pulse 1.5s ease-in-out infinite",
              }}
            />
          ) : isAuthenticated && user ? (
            <Link
              href="/profile"
              aria-label={`Hồ sơ cá nhân của ${user.displayName || user.email}`}
              title={user.displayName || user.email}
              style={{
                width: "40px",
                height: "40px",
                borderRadius: "50%",
                background: "#e4eee7",
                color: "#2d5d43",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                fontWeight: 600,
                fontSize: "15px",
                textDecoration: "none",
                overflow: "hidden",
                border: "1px solid rgba(45, 93, 67, 0.25)",
              }}
            >
              {user.avatarUrl ? (
                <Image
                  src={user.avatarUrl}
                  alt={user.displayName || "Avatar"}
                  width={40}
                  height={40}
                  unoptimized
                />
              ) : (
                <span>{initials}</span>
              )}
            </Link>
          ) : (
            <div style={{ display: "flex", alignItems: "center", gap: "8px" }}>
              <Link
                href="/login"
                style={{
                  padding: "8px 16px",
                  borderRadius: "10px",
                  fontSize: "14px",
                  fontWeight: 600,
                  color: "#1c2118",
                  textDecoration: "none",
                  border: "1px solid #b9b6a8",
                  background: "#ffffff",
                }}
              >
                Đăng nhập
              </Link>
              <Link
                href="/register"
                style={{
                  padding: "8px 16px",
                  borderRadius: "10px",
                  fontSize: "14px",
                  fontWeight: 600,
                  color: "#ffffff",
                  textDecoration: "none",
                  background: "#2d5d43",
                }}
              >
                Đăng ký
              </Link>
            </div>
          )}
        </div>
      </div>
    </header>
  );
}

