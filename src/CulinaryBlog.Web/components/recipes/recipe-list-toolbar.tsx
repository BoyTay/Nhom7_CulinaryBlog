"use client";

import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { useState, type FormEvent } from "react";
import type { CategoryDto } from "@/lib/api/contracts";
import type { RecipeDifficulty } from "@/lib/recipes/contracts";
import type { ListParams, ListSort, MaxCookMinutes } from "@/lib/recipes/list-view";
import "./recipe-list-toolbar.css";

const difficultyOptions: { value?: RecipeDifficulty; label: string }[] = [
  { label: "Tất cả" },
  { value: "Easy", label: "Dễ" },
  { value: "Medium", label: "Trung bình" },
  { value: "Hard", label: "Khó" },
];

const maxCookOptions: { value?: MaxCookMinutes; label: string }[] = [
  { label: "Bất kỳ" },
  { value: 30, label: "30 phút" },
  { value: 60, label: "60 phút" },
  { value: 90, label: "90 phút" },
];

const sortOptions: { value: ListSort; label: string }[] = [
  { value: "newest", label: "Mới nhất" },
  { value: "name", label: "Tên A–Z" },
  { value: "cook", label: "Thời gian nấu" },
];

export function RecipeListToolbar({ categories, params }: { categories: CategoryDto[]; params: ListParams }) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [query, setQuery] = useState(params.q);

  /** Any change keeps the other filters and goes back to page 1. */
  function update(changes: Record<string, string | undefined>) {
    const next = new URLSearchParams(searchParams.toString());
    next.delete("page");
    for (const [name, value] of Object.entries(changes)) {
      if (value === undefined || value === "") next.delete(name);
      else next.set(name, value);
    }
    const queryString = next.toString();
    router.push(queryString ? `${pathname}?${queryString}` : pathname);
  }

  function submitSearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    update({ q: query.trim() || undefined });
  }

  return (
    <div className="recipe-toolbar">
      <form role="search" className="recipe-toolbar-search" onSubmit={submitSearch}>
        <svg aria-hidden="true" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
          <circle cx="11" cy="11" r="7" />
          <path d="M20 20l-3.5-3.5" />
        </svg>
        <label htmlFor="recipe-search" className="visually-hidden">Tìm kiếm</label>
        <input
          id="recipe-search"
          type="search"
          value={query}
          onChange={(event) => setQuery(event.target.value)}
          placeholder="Thử gõ “pho bo” – tìm được cả “phở bò”"
          autoComplete="off"
        />
      </form>

      <div className="recipe-toolbar-filters">
        <div role="group" aria-label="Độ khó" className="recipe-toolbar-chips">
          <span>Độ khó</span>
          {difficultyOptions.map((option) => {
            const selected = params.difficulty === option.value;
            return (
              <button
                type="button"
                className="chip"
                aria-pressed={selected}
                key={option.label}
                onClick={() => update({ difficulty: option.value })}
              >
                {option.label}
              </button>
            );
          })}
        </div>

        <div className="recipe-toolbar-select">
          <label htmlFor="recipe-max-cook">Nấu tối đa</label>
          <select
            id="recipe-max-cook"
            value={params.maxCook ?? ""}
            onChange={(event) => update({ maxCook: event.target.value || undefined })}
          >
            {maxCookOptions.map((option) => (
              <option value={option.value ?? ""} key={option.label}>{option.label}</option>
            ))}
          </select>
        </div>

        <div className="recipe-toolbar-select">
          <label htmlFor="recipe-category">Danh mục</label>
          <select
            id="recipe-category"
            value={params.categoryId ?? ""}
            onChange={(event) => update({ categoryId: event.target.value || undefined })}
          >
            <option value="">Tất cả</option>
            {categories.map((category) => (
              <option value={category.id} key={category.id}>{category.name}</option>
            ))}
          </select>
        </div>

        <div className="recipe-toolbar-select recipe-toolbar-sort">
          <label htmlFor="recipe-sort">Sắp xếp</label>
          <select
            id="recipe-sort"
            value={params.sort}
            onChange={(event) => update({ sort: event.target.value === "newest" ? undefined : event.target.value })}
          >
            {sortOptions.map((option) => (
              <option value={option.value} key={option.value}>{option.label}</option>
            ))}
          </select>
        </div>
      </div>
    </div>
  );
}
