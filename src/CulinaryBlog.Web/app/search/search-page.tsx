"use client";

import { useQuery } from "@tanstack/react-query";
import Image from "next/image";
import Link from "next/link";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { useCallback, useEffect, useMemo, useRef, useState, type FormEvent } from "react";
import { ApiError, apiFetch } from "@/lib/api/client";
import type {
  CategoryDto,
  PagedResult,
  RecipeSearchItem,
  RecipeSearchOptions,
} from "@/lib/api/contracts";
import { Pagination, SkeletonCardGrid } from "@/components/shared";
import { highlightMatches } from "./highlight";
import { difficultyBadgeClass, ClockIcon } from "@/components/recipes/recipe-card";
import { buildSearchHref } from "@/lib/navigation/search-href";
import { recipeHref, routes } from "@/lib/navigation/routes";
import { queryKeys } from "@/lib/query/keys";
import { searchSummary } from "@/lib/search/search-summary";
import { cookTimeLabel, difficultyLabel } from "@/lib/ui/format";

const DEFAULT_PAGE_SIZE = 12;
const DEBOUNCE_MS = 350;
const SUGGESTION_COUNT = 4;
const popularSearches = ["Bún chả", "Món chay", "Cá kho", "Chè", "Canh chua", "Phở bò"];

function positiveInteger(value: string | null, fallback: number): number {
  if (!value || !/^\d+$/.test(value)) return fallback;
  const parsed = Number(value);
  return Number.isSafeInteger(parsed) && parsed > 0 ? parsed : fallback;
}

function errorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.status === 422) {
      return "Thông tin tìm kiếm chưa hợp lệ. Hãy kiểm tra từ khóa và bộ lọc.";
    }
    if (error.status >= 500) {
      return "Máy chủ đang gặp sự cố. Vui lòng thử lại sau.";
    }
    return error.message;
  }
  return "Không thể kết nối đến máy chủ. Hãy kiểm tra kết nối rồi thử lại.";
}

function makeSearchUrl(pathname: string, params: URLSearchParams): string {
  const query = params.toString();
  return query ? `${pathname}?${query}` : pathname;
}

export default function SearchPage() {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const searchParamsString = searchParams.toString();
  const [draftTerm, setDraftTerm] = useState(searchParams.get("q") ?? "");
  const [lastResponseMs, setLastResponseMs] = useState<number | null>(null);
  const [filtersOpen, setFiltersOpen] = useState(false);
  const requestNumber = useRef(0);

  const options = useMemo<RecipeSearchOptions>(() => {
    const params = new URLSearchParams(searchParamsString);
    return {
      q: (params.get("q") ?? "").trim(),
      categoryId: params.get("categoryId") ?? "",
      difficulty: params.get("difficulty") ?? "",
      maxCookTime: params.get("maxCookTime") ?? "",
      minServings: params.get("minServings") ?? "",
      sort: params.get("sort") ?? "-createdAt",
      page: positiveInteger(params.get("page"), 1),
      pageSize: positiveInteger(params.get("pageSize"), DEFAULT_PAGE_SIZE),
    };
  }, [searchParamsString]);

  const updateUrl = useCallback((mutate: (params: URLSearchParams) => void) => {
    const params = new URLSearchParams(searchParamsString);
    mutate(params);
    const nextUrl = makeSearchUrl(pathname, params);
    if (nextUrl !== makeSearchUrl(pathname, new URLSearchParams(searchParamsString))) {
      router.push(nextUrl, { scroll: false });
    }
  }, [pathname, router, searchParamsString]);

  useEffect(() => {
    setDraftTerm(options.q);
  }, [options.q]);

  useEffect(() => {
    const nextTerm = draftTerm.trim();
    if (nextTerm === options.q) return;

    const timer = window.setTimeout(() => {
      updateUrl((params) => {
        if (nextTerm) params.set("q", nextTerm);
        else params.delete("q");
        params.delete("page");
        if (!nextTerm && params.get("sort") === "relevance") {
          params.set("sort", "-createdAt");
        }
      });
    }, DEBOUNCE_MS);

    return () => window.clearTimeout(timer);
  }, [draftTerm, options.q, updateUrl]);

  const categoriesQuery = useQuery({
    queryKey: queryKeys.categories.all,
    queryFn: () => apiFetch<CategoryDto[]>("/categories"),
  });

  const recipesQuery = useQuery({
    queryKey: queryKeys.recipes.search(options),
    enabled: options.q.length === 0 || options.q.length >= 2,
    queryFn: async () => {
      const currentRequest = ++requestNumber.current;
      const startedAt = performance.now();
      const params = new URLSearchParams();
      if (options.q) params.set("q", options.q);
      if (options.categoryId) params.set("categoryId", options.categoryId);
      if (options.difficulty) params.set("difficulty", options.difficulty);
      if (options.maxCookTime) params.set("maxCookTime", options.maxCookTime);
      if (options.minServings) params.set("minServings", options.minServings);
      params.set("sort", options.sort);
      params.set("page", String(options.page));
      params.set("pageSize", String(options.pageSize));

      try {
        return await apiFetch<PagedResult<RecipeSearchItem>>(
          `/recipes/search?${params.toString()}`,
        );
      } finally {
        if (currentRequest === requestNumber.current) {
          setLastResponseMs(Math.round(performance.now() - startedAt));
        }
      }
    },
  });

  function changeFilter(key: string, value: string) {
    updateUrl((params) => {
      if (value) params.set(key, value);
      else params.delete(key);
      if (key !== "page") params.delete("page");
      if (key === "q" && !value && params.get("sort") === "relevance") {
        params.set("sort", "-createdAt");
      }
    });
  }

  function submitSearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const nextTerm = draftTerm.trim();
    updateUrl((params) => {
      if (nextTerm) params.set("q", nextTerm);
      else params.delete("q");
      params.delete("page");
      if (!nextTerm && params.get("sort") === "relevance") {
        params.set("sort", "-createdAt");
      }
    });
  }

  function resetSearch() {
    setDraftTerm("");
    router.push(pathname, { scroll: false });
  }

  const result = recipesQuery.data;
  const isShortTerm = options.q.length === 1;
  const isInitialLoading = recipesQuery.isPending && !isShortTerm;
  const summary = searchSummary(options.q, result?.totalCount ?? 0, options.sort);
  const activeFilters = [options.categoryId, options.difficulty, options.maxCookTime, options.minServings].filter(Boolean).length;
  const categoryName = (id: string) => categoriesQuery.data?.find((category) => category.id === id)?.name;
  const suggestions = options.q.length >= 2 ? (result?.items.slice(0, SUGGESTION_COUNT) ?? []) : [];

  return (
    <main>
      <section className="search-hero">
        <div className="search-hero-inner">
          <h1 className="rise">Tìm công thức</h1>
          <form className="rise search-form" onSubmit={submitSearch} role="search">
            <svg aria-hidden="true" className="search-form-icon" fill="none" height="22" stroke="currentColor" strokeLinecap="round" strokeWidth="2" viewBox="0 0 24 24" width="22">
              <circle cx="11" cy="11" r="7" />
              <path d="M20 20l-3.5-3.5" />
            </svg>
            <label className="visually-hidden" htmlFor="recipe-search">Từ khóa tìm công thức</label>
            <input
              autoComplete="off"
              id="recipe-search"
              maxLength={200}
              onChange={(event) => setDraftTerm(event.target.value)}
              placeholder="Ví dụ: phở bò, bánh mì..."
              type="search"
              value={draftTerm}
            />
            {draftTerm && (
              <button aria-label="Xóa từ khóa" className="search-clear" onClick={() => setDraftTerm("")} type="button">
                <svg aria-hidden="true" fill="none" height="18" stroke="currentColor" strokeLinecap="round" strokeWidth="2.2" viewBox="0 0 24 24" width="18"><path d="M6 6l12 12M18 6L6 18" /></svg>
              </button>
            )}
            <button className="btn btn-primary" type="submit" aria-label="Tìm kiếm">Tìm<span className="search-submit-word"> kiếm</span></button>
            {suggestions.length > 0 && (
              <ul aria-label="Gợi ý tìm kiếm" className="sg pop">
                {suggestions.map((recipe) => (
                  <li key={recipe.id}>
                    <Link href={recipeHref(recipe.slug)}>
                      <svg aria-hidden="true" fill="none" height="18" stroke="currentColor" strokeLinecap="round" strokeWidth="2" viewBox="0 0 24 24" width="18"><circle cx="11" cy="11" r="7" /><path d="M20 20l-3.5-3.5" /></svg>
                      <span>{highlightMatches(recipe.title, options.q)}</span>
                    </Link>
                  </li>
                ))}
              </ul>
            )}
          </form>
          <p className="search-hint" role={isShortTerm ? "status" : undefined}>
            <svg aria-hidden="true" fill="none" height="16" stroke="currentColor" strokeLinecap="round" strokeWidth="2" viewBox="0 0 24 24" width="16"><circle cx="12" cy="12" r="9" /><path d="M12 11v5M12 8h.01" /></svg>
            {isShortTerm ? "Từ khóa cần ít nhất 2 ký tự." : "Gõ không dấu cũng được, ví dụ “pho” sẽ ra “Phở bò”. Cần tối thiểu 2 ký tự."}
          </p>
        </div>
      </section>

      <div className="search-layout">
      <section className="search-column" aria-labelledby="search-summary">
      <div className="search-heading">
        <div>
          <h2 id="search-summary" aria-live="polite">{summary.title}</h2>
          {result && (
            <p className="search-meta">
              {summary.subtitle}
              {lastResponseMs !== null ? ` · ${lastResponseMs} ms` : ""}
            </p>
          )}
        </div>
        <button
          aria-controls="search-filters"
          aria-expanded={filtersOpen}
          className="btn btn-outline"
          onClick={() => setFiltersOpen((open) => !open)}
          type="button"
        >
          Bộ lọc{activeFilters > 0 ? ` (${activeFilters})` : ""}
        </button>
      </div>

      <section className="search-filters" hidden={!filtersOpen} id="search-filters" aria-label="Bộ lọc">
        <div className="field">
          <label htmlFor="filter-category">Danh mục</label>
          <select id="filter-category" onChange={(event) => changeFilter("categoryId", event.target.value)} value={options.categoryId}>
            <option value="">Tất cả danh mục</option>
            {categoriesQuery.data?.map((category) => (
              <option key={category.id} value={category.id}>{category.name}</option>
            ))}
          </select>
        </div>
        <div className="field">
          <label htmlFor="filter-difficulty">Độ khó</label>
          <select id="filter-difficulty" onChange={(event) => changeFilter("difficulty", event.target.value)} value={options.difficulty}>
            <option value="">Mọi độ khó</option>
            <option value="Easy">Dễ</option>
            <option value="Medium">Trung bình</option>
            <option value="Hard">Khó</option>
          </select>
        </div>
        <div className="field">
          <label htmlFor="filter-cook">Nấu tối đa (phút)</label>
          <input id="filter-cook" min="0" onChange={(event) => changeFilter("maxCookTime", event.target.value)} placeholder="Không giới hạn" type="number" value={options.maxCookTime} />
        </div>
        <div className="field">
          <label htmlFor="filter-servings">Khẩu phần tối thiểu</label>
          <input id="filter-servings" min="1" onChange={(event) => changeFilter("minServings", event.target.value)} placeholder="Bất kỳ" type="number" value={options.minServings} />
        </div>
        <div className="field">
          <label htmlFor="filter-sort">Sắp xếp</label>
          <select id="filter-sort" onChange={(event) => changeFilter("sort", event.target.value)} value={options.sort}>
            <option value="-createdAt">Mới nhất</option>
            <option value="title">Tên A–Z</option>
            <option value="-title">Tên Z–A</option>
            <option value="cookTime">Nấu nhanh nhất</option>
            <option value="-cookTime">Nấu lâu nhất</option>
            {options.q.length >= 2 && <option value="-relevance">Liên quan nhất</option>}
          </select>
        </div>
        <button className="btn btn-ghost" onClick={resetSearch} type="button">Xóa tất cả</button>
        {categoriesQuery.isError && (
          <p className="filter-error" role="alert">
            Không tải được danh mục.{" "}
            <button className="text-button" onClick={() => void categoriesQuery.refetch()} type="button">Thử lại</button>
          </p>
        )}
      </section>

      <div className="search-results" aria-busy={recipesQuery.isFetching}>
        {isInitialLoading && <SkeletonCardGrid cardHeight="8.5rem" count={3} label="Đang tải công thức" minColumnWidth="100%" />}

        {recipesQuery.isError && (
          <div className="empty-state" role="alert">
            <h2>Chưa thể tải kết quả</h2>
            <p>{errorMessage(recipesQuery.error)}</p>
            <button className="btn btn-primary search-retry" onClick={() => void recipesQuery.refetch()} type="button">Thử lại</button>
          </div>
        )}

        {!isInitialLoading && !recipesQuery.isError && isShortTerm && (
          <div className="empty-state">
            <h2>Nhập thêm một chút nhé</h2>
            <p>Từ khóa tìm kiếm cần ít nhất 2 ký tự.</p>
          </div>
        )}

        {!isInitialLoading && !recipesQuery.isError && !isShortTerm && result?.items.length === 0 && (
          <div className="empty-state">
            <h2>Chưa tìm thấy công thức phù hợp</h2>
            <p>Thử từ khóa ngắn hơn hoặc bỏ bớt bộ lọc.</p>
            <button className="btn btn-outline search-retry" onClick={resetSearch} type="button">Xóa bộ lọc</button>
          </div>
        )}

        {!isInitialLoading && !recipesQuery.isError && result && result.items.length > 0 && (
          <>
            {recipesQuery.isFetching && <p className="updating-indicator" role="status">Đang cập nhật kết quả…</p>}
            <ul className="search-rows">
              {result.items.map((recipe) => (
                <li key={recipe.id}>
                  <Link className="res search-row" href={recipeHref(recipe.slug)}>
                    <span className="search-row-thumb" aria-hidden={recipe.thumbnailUrl ? undefined : true}>
                      {recipe.thumbnailUrl && (
                        // Photos live in MinIO and already have resized variants; the browser loads them directly.
                        <Image src={recipe.thumbnailUrl} alt={`Ảnh món ${recipe.title}`} fill sizes="200px" unoptimized className="dish ph search-row-image" />
                      )}
                    </span>
                    <span className="search-row-body">
                      <span className="search-row-category">{categoryName(recipe.categoryId)}</span>
                      <h3>{highlightMatches(recipe.title, options.q)}</h3>
                      <span className="search-row-desc">{highlightMatches(recipe.description, options.q)}</span>
                      <span className="search-row-meta">
                        <span><ClockIcon />{cookTimeLabel(recipe.cookTime)}</span>
                        <span className={difficultyBadgeClass[recipe.difficulty]}>{difficultyLabel(recipe.difficulty)}</span>
                      </span>
                    </span>
                  </Link>
                </li>
              ))}
            </ul>
            {result.totalPages > 1 && (
              <Pagination
                label="Phân trang kết quả"
                onPageChange={(page) => changeFilter("page", String(page))}
                page={options.page}
                totalPages={result.totalPages}
              />
            )}
          </>
        )}
      </div>
      </section>

      <aside aria-label="Gợi ý" className="slide search-aside">
        <div className="search-aside-card">
          <h2>Được tìm nhiều</h2>
          <div className="search-popular">
            {popularSearches.map((term) => <Link className="search-popular-chip" href={buildSearchHref(term)} key={term}>{term}</Link>)}
          </div>
        </div>
        <div className="search-aside-tips">
          <h2>Mẹo tìm nhanh hơn</h2>
          <ul>
            <li>Tìm theo nguyên liệu: “sả”, “nấm”, “cá lóc”.</li>
            <li>Dùng trang <Link href={routes.recipes}>Công thức</Link> để lọc theo độ khó và thời gian nấu.</li>
            <li>Bỏ bớt từ khóa nếu không thấy kết quả.</li>
          </ul>
        </div>
      </aside>
      </div>
    </main>
  );
}
