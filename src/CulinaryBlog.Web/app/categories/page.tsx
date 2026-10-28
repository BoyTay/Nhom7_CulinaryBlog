import type { Metadata } from "next";
import Link from "next/link";
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
        <header className="recipe-list-heading">
          <h1>Danh mục</h1>
          <p>{categories ? `${categories.length} danh mục` : "Không tải được dữ liệu"}</p>
        </header>
        {categories === null ? (
          <Alert variant="error" title="Chưa thể tải danh mục">Máy chủ đang gặp sự cố. Vui lòng thử lại sau.</Alert>
        ) : categories.length === 0 ? (
          <section className="recipe-list-empty"><h2>Chưa có danh mục nào</h2></section>
        ) : (
          <ul className="recipe-list-grid" aria-label="Danh sách danh mục">
            {categories.map((category) => (
              <li key={category.id} style={{ listStyle: "none" }}>
                <Link className="recipe-list-card" href={categoryHref(category.slug)}>
                  <div className="recipe-list-card-body">
                    <h2>{category.name}</h2>
                    {category.description && <p>{category.description}</p>}
                    <div className="recipe-list-card-meta"><span>{category.recipeCount} công thức</span></div>
                  </div>
                </Link>
              </li>
            ))}
          </ul>
        )}
      </main>
    </div>
  );
}
