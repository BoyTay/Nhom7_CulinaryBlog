import type { Metadata } from "next";
import Link from "next/link";
import { Alert, SiteHeader } from "@/components/shared";
import { RecipeListFilters } from "@/components/recipes/recipe-list-filters";
import { RecipeListPagination } from "@/components/recipes/recipe-list-pagination";
import { RecipeListResults } from "@/components/recipes/recipe-list-results";
import {
  createRecipeListViewModel,
  paginateLocally,
  parseRecipeListFilters,
  toPublicRecipeFilters,
  type RecipeListSearchParams,
} from "@/lib/recipes/list";
import { getPublicRecipeList } from "@/lib/recipes/server-list";

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  title: { absolute: "Công thức | Culinary Blog" },
  description: "Khám phá các công thức nấu ăn đã xuất bản trên Culinary Blog.",
};

export default async function RecipesPage({
  searchParams,
}: {
  searchParams: Promise<RecipeListSearchParams>;
}) {
  const parsed = parseRecipeListFilters(await searchParams);

  if (!parsed.success) {
    return (
      <RecipeListShell countLabel="Bộ lọc không hợp lệ">
        <Alert
          variant="warning"
          title="Không thể áp dụng bộ lọc"
          action={<Link className="recipe-list-text-action" href="/recipes">Xóa bộ lọc</Link>}
        >
          {parsed.message}
        </Alert>
      </RecipeListShell>
    );
  }

  try {
    const data = await getPublicRecipeList(toPublicRecipeFilters(parsed.value));
    const localPage = paginateLocally(data.recipes, parsed.value.page);
    const model = createRecipeListViewModel(localPage.items);

    return (
      <RecipeListShell countLabel={`${localPage.totalCount.toLocaleString("vi-VN")} công thức`}>
        <RecipeListFilters categories={data.categories} filters={parsed.value} />
        <RecipeListResults model={model} />
        {model.state === "populated" && localPage.totalPages > 1 && (
          <RecipeListPagination
            filters={parsed.value}
            page={localPage.page}
            totalPages={localPage.totalPages}
          />
        )}
      </RecipeListShell>
    );
  } catch {
    return (
      <RecipeListShell countLabel="Không tải được dữ liệu">
        <RecipeListResults model={createRecipeListViewModel([], true)} />
      </RecipeListShell>
    );
  }
}

function RecipeListShell({
  children,
  countLabel,
}: {
  children: React.ReactNode;
  countLabel: string;
}) {
  return (
    <div className="recipes-page ds-scope">
      <SiteHeader />
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
