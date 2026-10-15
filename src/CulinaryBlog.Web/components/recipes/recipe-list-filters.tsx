"use client";

import { usePathname, useRouter } from "next/navigation";
import type { CategoryDto } from "@/lib/api/contracts";
import type { RecipeDifficulty } from "@/lib/recipes/contracts";
import type { ParsedRecipeListFilters } from "@/lib/recipes/list";

const difficultyOptions: { value?: RecipeDifficulty; label: string }[] = [
  { label: "Tất cả" },
  { value: "Easy", label: "Dễ" },
  { value: "Medium", label: "Trung bình" },
  { value: "Hard", label: "Khó" },
];

export function RecipeListFilters({
  categories,
  filters,
}: {
  categories: CategoryDto[];
  filters: ParsedRecipeListFilters;
}) {
  const router = useRouter();
  const pathname = usePathname();

  function updateFilter(name: "categoryId" | "difficulty", value?: string) {
    const params = new URLSearchParams();
    const categoryId = name === "categoryId" ? value : filters.categoryId;
    const difficulty = name === "difficulty" ? value : filters.difficulty;

    if (categoryId) params.set("categoryId", categoryId);
    if (difficulty) params.set("difficulty", difficulty);
    const query = params.toString();
    router.push(query ? `${pathname}?${query}` : pathname);
  }

  return (
    <section className="recipe-list-filters" aria-label="Bộ lọc công thức">
      <div className="recipe-list-difficulty" role="group" aria-label="Độ khó">
        <span>Độ khó</span>
        {difficultyOptions.map((option) => {
          const selected = filters.difficulty === option.value;
          return (
            <button
              type="button"
              className="recipe-filter-chip"
              data-selected={selected || undefined}
              aria-pressed={selected}
              key={option.label}
              onClick={() => updateFilter("difficulty", option.value)}
            >
              {option.label}
            </button>
          );
        })}
      </div>

      <label className="recipe-list-category">
        <span>Danh mục</span>
        <select
          value={filters.categoryId ?? ""}
          onChange={(event) => updateFilter("categoryId", event.target.value || undefined)}
        >
          <option value="">Tất cả danh mục</option>
          {categories.map((category) => (
            <option value={category.id} key={category.id}>{category.name}</option>
          ))}
        </select>
      </label>
    </section>
  );
}
