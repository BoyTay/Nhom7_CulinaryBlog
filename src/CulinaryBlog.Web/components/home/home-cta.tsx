"use client";

import Link from "next/link";
import { useAuth } from "@/lib/auth/use-auth";
import { routes } from "@/lib/navigation/routes";

export function HomeCta() {
  const { isAuthenticated, isLoading } = useAuth();
  if (isLoading) return null;

  return (
    <section className="home-cta" aria-labelledby="home-cta-title">
      <h2 id="home-cta-title">
        {isAuthenticated ? "Có món nào bạn muốn chia sẻ?" : "Chia sẻ công thức của riêng bạn"}
      </h2>
      <p>
        {isAuthenticated
          ? "Viết công thức mới, thêm ảnh và xuất bản khi bạn sẵn sàng."
          : "Tạo tài khoản miễn phí để viết công thức, thêm ảnh và xuất bản cho mọi người."}
      </p>
      <Link
        className="button button-primary"
        href={isAuthenticated ? routes.dashboardRecipeNew : routes.register}
      >
        {isAuthenticated ? "Viết công thức" : "Tạo tài khoản"}
      </Link>
    </section>
  );
}
