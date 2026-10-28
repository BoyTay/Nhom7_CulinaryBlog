import type { Metadata } from "next";
import { CategoryCard } from "@/components/categories/category-card";
import { Alert } from "@/components/shared";
import { categoryHref } from "@/lib/navigation/routes";
import { getCategoryList } from "@/lib/categories/server-categories";
import "../recipes/recipes.css";

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  title: { absolute: "Danh mục | Culinary Blog" },
  description: "Duyệt công thức nấu ăn theo danh mục món ăn.",
};

export default async function CategoriesPage() {
  const categories = await getCategoryList().catch(() => null);

  return (
    <div className="recipes-page">
      <main className="recipe-list-main">
        <header className="recipe-list-heading categories-heading">
          <div>
            <h1>Danh mục</h1>
            <p>Chọn một danh mục để xem các công thức đã xuất bản.</p>
          </div>
        </header>
        {categories === null ? (
          <Alert variant="error" title="Chưa thể tải danh mục">Máy chủ đang gặp sự cố. Vui lòng thử lại sau.</Alert>
        ) : categories.length === 0 ? (
          <section className="empty-state">
            <h2>Chưa có danh mục nào</h2>
          </section>
        ) : (
          <div className="recipe-list-grid" role="list" aria-label="Danh sách danh mục">
            {categories.map((category) => (
              <div role="listitem" key={category.id} className="category-list-item">
                <CategoryCard
                  name={category.name}
                  href={categoryHref(category.slug)}
                  description={category.description}
                  recipeCount={category.recipeCount}
                />
              </div>
            ))}
          </div>
        )}
      </main>
    </div>
  );
}
