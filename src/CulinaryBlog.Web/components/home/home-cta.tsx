"use client";

import Link from "next/link";
import { useAuth } from "@/lib/auth/use-auth";
import { routes } from "@/lib/navigation/routes";

const steps = [
  { title: "Soạn bản nháp", text: "Nhập thông tin, nguyên liệu và hình ảnh. Mọi thứ được lưu ở trạng thái nháp." },
  { title: "Thêm bước & hẹn giờ", text: "Mô tả từng bước làm, gắn thời gian để người nấu bấm giờ ngay trên trang." },
  { title: "Xuất bản khi sẵn sàng", text: "Hệ thống kiểm tra đủ nguyên liệu và các bước trước khi công thức lên trang chủ." },
];

export function HomeCta() {
  const { isAuthenticated } = useAuth();

  return (
    <section className="rv home-cta-wrap" aria-labelledby="home-cta-title">
      <div className="home-cta">
        <div className="home-cta-copy">
          <span>Dành cho tác giả</span>
          <h2 id="home-cta-title">Có món tủ muốn chia sẻ? <span className="home-desktop-word">Đăng công thức trong ba bước.</span></h2>
          <p>Đăng ký miễn phí, soạn nháp thoải mái và chỉ xuất bản khi bạn thấy sẵn sàng.</p>
          <Link className="btn btn-lg btn-gold" href={isAuthenticated ? routes.dashboardRecipeNew : routes.register}>
            {isAuthenticated ? "Viết công thức mới" : "Tạo tài khoản tác giả"}
          </Link>
        </div>
        <ol>
          {steps.map((step, index) => (
            <li key={step.title}>
              <span>{index + 1}</span>
              <span><strong>{step.title}</strong><small>{step.text}</small></span>
            </li>
          ))}
        </ol>
      </div>
    </section>
  );
}
