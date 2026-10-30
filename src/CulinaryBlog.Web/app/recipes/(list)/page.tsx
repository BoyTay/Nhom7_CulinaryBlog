import type { Metadata } from "next";
import Link from "next/link";
import { Alert } from "@/components/shared";
import { QueryPagination } from "@/components/shared/query-pagination";
import { RecipeListResults } from "@/components/recipes/recipe-list-results";
import { RecipeListToolbar, RecipeSortSelect } from "@/components/recipes/recipe-list-toolbar";
import { routes } from "@/lib/navigation/routes";
import { createRecipeListViewModel, type RecipeListSearchParams } from "@/lib/recipes/list";
import { applyListView, parseListParams, type ListParams } from "@/lib/recipes/list-view";
import { getPublicRecipeList } from "@/lib/recipes/server-list";

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  title: { absolute: "Công thức | Culinary Blog" },
  description: "Khám phá các công thức nấu ăn đã xuất bản trên Culinary Blog.",
};

export default async function RecipesPage({ searchParams }: { searchParams: Promise<RecipeListSearchParams> }) {
  const params = parseListParams(await searchParams);

  try {
    const data = await getPublicRecipeList({ status: "Published" });
    const view = applyListView(data.recipes, params);
    const model = createRecipeListViewModel(view.items);
    const summary = view.totalCount === 0
      ? "Không có kết quả"
      : `Hiển thị ${view.items.length} trong ${view.totalCount.toLocaleString("vi-VN")} công thức`;

    return (
      <RecipeListShell params={params}>
        <RecipeListToolbar categories={data.categories} params={params} />
        <section aria-labelledby="recipe-results-title" className="recipe-list-results">
          <h2 id="recipe-results-title" aria-live="polite">{summary}</h2>
          <RecipeListResults model={model} />
          {model.state === "populated" && view.totalPages > 1 && (
            <QueryPagination label="Phân trang công thức" page={view.page} totalPages={view.totalPages} />
          )}
        </section>
      </RecipeListShell>
    );
  } catch {
    return (
      <RecipeListShell params={params}>
        <section className="recipe-list-results">
          <Alert variant="error" title="Chưa thể tải công thức" action={<Link href={routes.recipes}>Thử lại</Link>}>
            Máy chủ đang gặp sự cố. Vui lòng thử lại sau.
          </Alert>
        </section>
      </RecipeListShell>
    );
  }
}

function RecipeListShell({ children, params }: { children: React.ReactNode; params: ListParams }) {
  return (
    <div className="recipes-page">
      <main className="recipe-list-main">
        <nav aria-label="Đường dẫn" className="breadcrumb">
          <Link href={routes.home}>Trang chủ</Link><span aria-hidden="true">/</span><span aria-current="page">Công thức</span>
        </nav>
        <header className="recipe-list-heading">
          <div>
            <h1 className="rise">Tất cả công thức</h1>
            <p>Lọc theo danh mục, độ khó và thời gian nấu để tìm món vừa với bữa cơm hôm nay.</p>
          </div>
          <RecipeSortSelect params={params} />
        </header>
        <div className="recipe-list-layout">{children}</div>
      </main>
    </div>
  );
}
