"use client";

import Link from "next/link";
import { useAuth } from "@/lib/auth/use-auth";

export function UserNav() {
  const { user, isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return (
      <span
        style={{
          width: "32px",
          height: "32px",
          borderRadius: "50%",
          background: "var(--color-muted, #e4eee7)",
          display: "inline-block",
          opacity: 0.6,
        }}
        aria-hidden="true"
      />
    );
  }

  if (isAuthenticated && user) {
    const initials = (user.displayName || user.email || "CB")
      .slice(0, 2)
      .toUpperCase();

    return (
      <div style={{ display: "inline-flex", alignItems: "center", gap: "0.75rem" }}>
        <Link
          href="/profile"
          style={{
            display: "inline-flex",
            alignItems: "center",
            gap: "0.5rem",
            color: "inherit",
            fontWeight: 600,
            textDecoration: "none",
          }}
          title={user.displayName || user.email}
        >
          <span
            style={{
              width: "32px",
              height: "32px",
              borderRadius: "50%",
              background: "#2d5d43",
              color: "#ffffff",
              display: "inline-flex",
              alignItems: "center",
              justifyContent: "center",
              fontSize: "12px",
              fontWeight: 700,
            }}
          >
            {initials}
          </span>
          <span style={{ fontSize: "0.95rem" }}>{user.displayName || user.email}</span>
        </Link>
      </div>
    );
  }

  return (
    <div style={{ display: "inline-flex", alignItems: "center", gap: "0.75rem" }}>
      <Link href="/login" style={{ color: "inherit", textDecoration: "none" }}>
        Đăng nhập
      </Link>
      <Link
        href="/register"
        style={{
          background: "#2d5d43",
          color: "#ffffff",
          padding: "0.4rem 0.85rem",
          borderRadius: "12px",
          textDecoration: "none",
          fontSize: "0.9rem",
          fontWeight: 600,
        }}
      >
        Đăng ký
      </Link>
    </div>
  );
}

