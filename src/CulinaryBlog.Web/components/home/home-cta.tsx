"use client";

import Link from "next/link";
import { useAuth } from "@/lib/auth/use-auth";
import { routes } from "@/lib/navigation/routes";

export function HomeCta() {
  const { isAuthenticated, isLoading } = useAuth();
  if (isLoading) return null;

  return (
    <section className="home-cta" aria-labelledby="home-cta-title">
      <div>
        <h2 id="home-cta-title">Có món tủ muốn chia sẻ?</h2>
        <p>
          {isAuthenticated
            ? "Viết công thức mới, thêm ảnh và xuất bản khi bạn sẵn sàng."
            : "Đăng ký miễn phí, thêm nguyên liệu, từng bước thực hiện và ảnh món ăn, rồi xuất bản khi sẵn sàng."}
        </p>
      </div>
      <Link className="btn btn-lg home-cta-button" href={isAuthenticated ? routes.dashboardRecipeNew : routes.register}>
        {isAuthenticated ? "Viết công thức mới" : "Tạo tài khoản Tác giả"}
      </Link>
    </section>
  );
}
