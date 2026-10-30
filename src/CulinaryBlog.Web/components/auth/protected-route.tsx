"use client";

import { usePathname, useRouter } from "next/navigation";
import { useEffect, type ReactNode } from "react";
import { authStore } from "@/lib/auth/auth-store";
import { useAuth } from "@/lib/auth/use-auth";

export interface ProtectedRouteProps {
  children: ReactNode;
  /**
   * Optional array of required roles (e.g. ["Admin", "Chef"]).
   * If provided, the user must have at least one of these roles.
   */
  requiredRoles?: string[];
  /**
   * Fallback URL for unauthorized access. Defaults to "/login".
   */
  fallbackUrl?: string;
}

export function ProtectedRoute({
  children,
  requiredRoles,
  fallbackUrl = "/login",
}: ProtectedRouteProps) {
  const router = useRouter();
  const pathname = usePathname();
  const { user, isAuthenticated, isLoading } = useAuth();

  useEffect(() => {
    if (isLoading) return;

    if (!isAuthenticated) {
      // After a deliberate sign-out go home; only a lost session returns to the login page for this URL.
      if (authStore.consumeIntentionalSignOut()) {
        router.replace("/");
        return;
      }
      const returnUrl = encodeURIComponent(pathname);
      router.replace(`${fallbackUrl}?returnUrl=${returnUrl}`);
    }
    // A signed-in user without the required role stays here and sees the "access denied" message below.
  }, [isLoading, isAuthenticated, fallbackUrl, pathname, router]);

  // Loading state skeleton to avoid Flash of Unauthenticated Content (FOUC)
  if (isLoading) {
    return (
      <div
        style={{
          minHeight: "80vh",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          padding: "2rem",
        }}
        aria-busy="true"
        aria-label="Đang xác thực phiên đăng nhập..."
      >
        <div
          style={{
            maxWidth: "480px",
            width: "100%",
            background: "#ffffff",
            border: "1px solid #e8decb",
            borderRadius: "20px",
            padding: "2.5rem",
            display: "flex",
            flexDirection: "column",
            alignItems: "center",
            gap: "1.25rem",
          }}
        >
          <div
            style={{
              width: "56px",
              height: "56px",
              borderRadius: "50%",
              background: "#fce9dd",
              animation: "pulse 1.5s ease-in-out infinite",
            }}
          />
          <div
            style={{
              width: "180px",
              height: "20px",
              borderRadius: "8px",
              background: "#ede6d8",
            }}
          />
          <div
            style={{
              width: "240px",
              height: "14px",
              borderRadius: "6px",
              background: "#f4efe4",
            }}
          />
        </div>
      </div>
    );
  }

  // Not authenticated
  if (!isAuthenticated) {
    return null;
  }

  // Role check failed
  if (requiredRoles && requiredRoles.length > 0) {
    const userRoles = user?.roles ?? [];
    const hasPermission = requiredRoles.some((role) =>
      userRoles.map((r) => r.toLowerCase()).includes(role.toLowerCase()),
    );
    if (!hasPermission) {
      return (
        <div
            style={{
            minHeight: "70vh",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            padding: "2rem",
          }}
        >
          <div
            style={{
              maxWidth: "520px",
              width: "100%",
              background: "#ffffff",
              border: "1px solid #e8decb",
              borderRadius: "20px",
              padding: "2.5rem",
              textAlign: "center",
              display: "flex",
              flexDirection: "column",
              gap: "1rem",
            }}
            role="alert"
          >
            <h2 style={{ margin: 0, fontSize: "1.5rem", color: "#b42318" }}>
              Quyền truy cập bị từ chối
            </h2>
            <p style={{ color: "#6b5a4c", margin: 0 }}>
              Bạn không có đủ quyền hạn để truy cập vào khu vực này.
            </p>
          </div>
        </div>
      );
    }
  }

  return <>{children}</>;
}

