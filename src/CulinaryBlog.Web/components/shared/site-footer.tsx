import Link from "next/link";
import { routes } from "@/lib/navigation/routes";

export function SiteFooter() {
  return (
    <footer className="site-footer">
      <div className="site-footer-inner">
        <nav aria-label="Liên kết cuối trang">
          <Link href={routes.recipes}>Công thức</Link>
          <Link href={routes.categories}>Danh mục</Link>
          <Link href={routes.search}>Tìm kiếm</Link>
        </nav>
        <p>Nhóm 7 · Advanced Web Application Development · 2026–2027</p>
      </div>
    </footer>
  );
}
