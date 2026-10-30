import type { Metadata } from "next";
import Link from "next/link";
import { CategoryCard } from "@/components/categories/category-card";
import { Alert } from "@/components/shared";
import { categoryHref, routes } from "@/lib/navigation/routes";
import { getCategoryList } from "@/lib/categories/server-categories";
import "../recipes/recipes.css";

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  title: { absolute: "Danh mục | Culinary Blog" },
  description: "Duyệt công thức nấu ăn theo danh mục món ăn.",
};

export default async function CategoriesPage() {
  const categories = await getCategoryList().catch(() => null);
  const total = categories?.reduce((sum, category) => sum + category.recipeCount, 0) ?? 0;

  return (
    <div className="recipes-page">
      <main className="recipe-list-main">
        <nav aria-label="Đường dẫn" className="breadcrumb">
          <Link href={routes.home}>Trang chủ</Link><span aria-hidden="true">/</span><span aria-current="page">Danh mục</span>
        </nav>
        <header className="rise categories-heading">
          <h1>Danh mục</h1>
          <p>Chọn một nhóm món để xem các công thức trong đó. Hiện có {total} công thức được xuất bản.</p>
        </header>
        {categories === null ? (
          <Alert variant="error" title="Chưa thể tải danh mục">Máy chủ đang gặp sự cố. Vui lòng thử lại sau.</Alert>
        ) : categories.length === 0 ? (
          <section className="empty-state">
            <h2>Chưa có danh mục nào</h2>
          </section>
        ) : (
          <ul className="category-grid" aria-label="Danh sách danh mục">
            {categories.map((category) => (
              <li key={category.id} className="rise category-list-item">
                <CategoryCard
                  name={category.name}
                  href={categoryHref(category.slug)}
                  description={category.description}
                  imageUrl={category.imageUrl}
                  recipeCount={category.recipeCount}
                />
              </li>
            ))}
          </ul>
        )}
      </main>
    </div>
  );
}
