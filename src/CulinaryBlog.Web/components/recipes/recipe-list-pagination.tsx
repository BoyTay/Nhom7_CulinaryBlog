"use client";

import { usePathname, useRouter } from "next/navigation";
import { Pagination } from "@/components/shared";
import type { ParsedRecipeListFilters } from "@/lib/recipes/list";

export function RecipeListPagination({
  filters,
  page,
  totalPages,
}: {
  filters: ParsedRecipeListFilters;
  page: number;
  totalPages: number;
}) {
  const pathname = usePathname();
  const router = useRouter();

  function changePage(nextPage: number) {
    const params = new URLSearchParams();
    if (filters.categoryId) params.set("categoryId", filters.categoryId);
    if (filters.difficulty) params.set("difficulty", filters.difficulty);
    if (nextPage > 1) params.set("page", String(nextPage));
    const query = params.toString();
    router.push(query ? `${pathname}?${query}` : pathname);
  }

  return (
    <Pagination
      label="Phân trang công thức"
      onPageChange={changePage}
      page={page}
      totalPages={totalPages}
    />
  );
}
