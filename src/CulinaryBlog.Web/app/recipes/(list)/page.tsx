import type { Metadata } from "next";
import Link from "next/link";
import { Alert } from "@/components/shared";
import { QueryPagination } from "@/components/shared/query-pagination";
import { RecipeListResults } from "@/components/recipes/recipe-list-results";
import { RecipeListToolbar } from "@/components/recipes/recipe-list-toolbar";
import { routes } from "@/lib/navigation/routes";
import { createRecipeListViewModel, type RecipeListSearchParams } from "@/lib/recipes/list";
import { applyListView, parseListParams } from "@/lib/recipes/list-view";
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

    return (
      <RecipeListShell countLabel={`${view.totalCount.toLocaleString("vi-VN")} công thức`}>
        <RecipeListToolbar categories={data.categories} params={params} />
        <RecipeListResults model={model} />
        {model.state === "populated" && view.totalPages > 1 && (
          <QueryPagination label="Phân trang công thức" page={view.page} totalPages={view.totalPages} />
        )}
      </RecipeListShell>
    );
  } catch {
    return (
      <RecipeListShell countLabel="Không tải được dữ liệu">
        <Alert variant="error" title="Chưa thể tải công thức" action={<Link href={routes.recipes}>Thử lại</Link>}>
          Máy chủ đang gặp sự cố. Vui lòng thử lại sau.
        </Alert>
      </RecipeListShell>
    );
  }
}

function RecipeListShell({ children, countLabel }: { children: React.ReactNode; countLabel: string }) {
  return (
    <div className="recipes-page">
      <main className="recipe-list-main">
        <header className="recipe-list-heading">
          <h1>Tất cả công thức</h1>
          <p aria-live="polite">{countLabel}</p>
        </header>
        {children}
      </main>
    </div>
  );
}
