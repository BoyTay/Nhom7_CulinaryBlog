import Link from "next/link";
import { routes } from "@/lib/navigation/routes";
import { BrandLogo } from "./brand-logo";

const columns = [
  {
    title: "Khám phá",
    links: [
      { label: "Công thức", href: routes.recipes },
      { label: "Danh mục", href: routes.categories },
      { label: "Tìm kiếm", href: routes.search },
    ],
  },
  {
    title: "Tài khoản",
    links: [
      { label: "Đăng nhập", href: routes.login },
      { label: "Đăng ký", href: routes.register },
      { label: "Quên mật khẩu", href: routes.forgotPassword },
      { label: "Hồ sơ", href: routes.profile },
    ],
  },
  {
    title: "Dành cho tác giả",
    links: [
      { label: "Viết công thức", href: routes.dashboardRecipeNew },
      { label: "Công thức của tôi", href: routes.dashboardRecipes },
    ],
  },
];

export function SiteFooter() {
  return (
    <footer className="ft">
      <div className="ft-in">
        <div className="ft-brand">
          <strong><BrandLogo size={40} />Culinary Blog</strong>
          <span>Khám phá, chia sẻ và lưu giữ những công thức đáng nhớ.</span>
        </div>
        <nav aria-label="Chân trang" className="ft-cols">
          {columns.map((column) => (
            <div className="ft-col" key={column.title}>
              <strong>{column.title}</strong>
              {column.links.map((link) => <Link href={link.href} key={link.href}>{link.label}</Link>)}
            </div>
          ))}
        </nav>
      </div>
      <div className="ft-bar">
        <span>© 2026 Culinary Blog · Nhóm 07 · Phát triển ứng dụng web nâng cao</span>
        <details>
          <summary>Nguồn ảnh</summary>
          <p>Ảnh món ăn từ Wikimedia Commons: Andy Li (phở, CC0) · Phương Huy (cơm tấm, CC0; bún chả, CC BY-SA 4.0) · Minh Nguyen (bánh mì, CC BY-SA 4.0) · Baoothersks (bún bò Huế, CC BY-SA 4.0) · Frank Fox (cà phê, CC BY-SA 4.0) · Zxcvasdfqwer888 (chè, CC BY-SA 3.0) · Orderinchaos (bánh xèo, CC BY-SA 4.0) · Tran Hai Duong (gỏi cuốn, CC0) · Binh Giang (cá kho tộ, phạm vi công cộng) · Jason Hutchens (canh chua, CC BY 2.0) · M Ameen (chả giò, CC0) · pelican from Tokyo (phở, CC BY-SA 2.0) · Viethavvh (rau thơm, CC BY-SA 3.0).</p>
        </details>
      </div>
    </footer>
  );
}

/** One-line footer for the login-style pages, which have no site header. */
export function MiniFooter({ onDark = false }: { onDark?: boolean }) {
  return (
    <footer className={onDark ? "ft-mini au-ft" : "ft-mini"}>
      <span>© 2026 Culinary Blog · Nhóm 07</span>
      <span>
        <Link href={routes.recipes}>Công thức</Link> · <Link href={routes.categories}>Danh mục</Link> · <Link href={routes.search}>Tìm kiếm</Link>
      </span>
    </footer>
  );
}
