"use client";

import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { useState, type FormEvent } from "react";
import type { CategoryDto } from "@/lib/api/contracts";
import type { RecipeDifficulty } from "@/lib/recipes/contracts";
import type { ListParams, ListSort, MaxCookMinutes } from "@/lib/recipes/list-view";
import "./recipe-list-toolbar.css";

const difficultyOptions: { value: RecipeDifficulty; label: string; className: string }[] = [
  { value: "Easy", label: "Dễ", className: "badge-easy" },
  { value: "Medium", label: "Trung bình", className: "badge-medium" },
  { value: "Hard", label: "Khó", className: "badge-hard" },
];

const maxCookOptions: { value: MaxCookMinutes; label: string }[] = [
  { value: 15, label: "≤ 15 phút" },
  { value: 30, label: "≤ 30 phút" },
  { value: 60, label: "≤ 60 phút" },
  { value: 90, label: "≤ 90 phút" },
];

const sortOptions: { value: ListSort; label: string }[] = [
  { value: "newest", label: "Mới nhất" },
  { value: "name", label: "Tên A–Z" },
  { value: "cook", label: "Nấu nhanh nhất" },
];

function useListUrl() {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  /** Any change keeps the other filters and goes back to page 1. */
  return (changes: Record<string, string | undefined>) => {
    const next = new URLSearchParams(searchParams.toString());
    next.delete("page");
    for (const [name, value] of Object.entries(changes)) {
      if (value === undefined || value === "") next.delete(name);
      else next.set(name, value);
    }
    const queryString = next.toString();
    router.push(queryString ? `${pathname}?${queryString}` : pathname);
  };
}

export function RecipeSortSelect({ params }: { params: ListParams }) {
  const update = useListUrl();
  return (
    <label className="recipe-sort">
      Sắp xếp
      <select
        id="recipe-sort"
        value={params.sort}
        onChange={(event) => update({ sort: event.target.value === "newest" ? undefined : event.target.value })}
      >
        {sortOptions.map((option) => <option value={option.value} key={option.value}>{option.label}</option>)}
      </select>
    </label>
  );
}

export function RecipeListToolbar({ categories, params }: { categories: CategoryDto[]; params: ListParams }) {
  const update = useListUrl();
  const [query, setQuery] = useState(params.q);
  const total = categories.reduce((sum, category) => sum + category.recipeCount, 0);

  function submitSearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    update({ q: query.trim() || undefined });
  }

  return (
    <aside aria-label="Bộ lọc" className="slide recipe-filters">
      <form role="search" className="sf recipe-filter-search" onSubmit={submitSearch}>
        <svg aria-hidden="true" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round"><circle cx="11" cy="11" r="7" /><path d="M20 20l-3.5-3.5" /></svg>
        <label htmlFor="recipe-search" className="visually-hidden">Tìm kiếm</label>
        <input
          id="recipe-search"
          type="search"
          value={query}
          onChange={(event) => setQuery(event.target.value)}
          placeholder="Thử “pho bo”"
          autoComplete="off"
        />
      </form>

      <fieldset>
        <legend>Danh mục</legend>
        <div className="recipe-filter-list">
          <button type="button" className="opt recipe-filter-row" aria-pressed={!params.categoryId} onClick={() => update({ categoryId: undefined })}>
            <span>Tất cả</span><small>{total}</small>
          </button>
          {categories.map((category) => (
            <button
              type="button"
              className="opt recipe-filter-row"
              aria-pressed={params.categoryId === category.id}
              key={category.id}
              onClick={() => update({ categoryId: params.categoryId === category.id ? undefined : category.id })}
            >
              <span>{category.name}</span><small>{category.recipeCount}</small>
            </button>
          ))}
        </div>
      </fieldset>

      <fieldset>
        <legend>Độ khó</legend>
        <div className="recipe-filter-levels">
          {difficultyOptions.map((option) => {
            const selected = params.difficulty === option.value;
            return (
              <button
                type="button"
                className={selected ? `opt recipe-filter-level ${option.className}` : "opt recipe-filter-level"}
                aria-pressed={selected}
                key={option.value}
                onClick={() => update({ difficulty: selected ? undefined : option.value })}
              >
                {option.label}
              </button>
            );
          })}
        </div>
      </fieldset>

      <fieldset>
        <legend>Thời gian nấu tối đa</legend>
        <div className="recipe-filter-times">
          {maxCookOptions.map((option) => {
            const selected = params.maxCook === option.value;
            return (
              <button
                type="button"
                className="opt recipe-filter-time"
                aria-pressed={selected}
                key={option.value}
                onClick={() => update({ maxCook: selected ? undefined : String(option.value) })}
              >
                {option.label}
              </button>
            );
          })}
        </div>
      </fieldset>

      <button
        type="button"
        className="btn btn-outline"
        onClick={() => {
          setQuery("");
          update({ q: undefined, categoryId: undefined, difficulty: undefined, maxCook: undefined });
        }}
      >
        Xóa bộ lọc
      </button>
    </aside>
  );
}
