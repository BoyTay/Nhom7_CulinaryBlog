"use client";

import Link from "next/link";
import { RecipeImage } from "@/components/recipes/recipe-image";
import { Skeleton } from "@/components/shared";
import { useAuth } from "@/lib/auth/use-auth";
import { countMissingIngredients, draftNote, summarizeMyRecipes } from "@/lib/dashboard/my-recipes";
import type { RecipeListItemDto, RecipeStatus } from "@/lib/recipes/contracts";
import { routes } from "@/lib/navigation/routes";
import { relativeTimeVi } from "@/lib/ui/format";
import { useDraftGaps, useMyRecipes } from "./my-recipes-table";

const TODO_COUNT = 3;
const ACTIVITY_COUNT = 4;

const activity: Record<RecipeStatus, { label: string; color: string }> = {
  Published: { label: "Đã xuất bản", color: "#2f5d3a" },
  Draft: { label: "Lưu bản nháp", color: "#b58a1b" },
  Archived: { label: "Chuyển vào lưu trữ", color: "#8a2a3a" },
};

function lastTouched(recipe: RecipeListItemDto): string {
  return recipe.updatedAt ?? recipe.createdAt;
}

export function DashboardGreeting() {
  const { user } = useAuth();
  return <h1 className="rise">Xin chào{user?.displayName ? `, ${user.displayName}` : ""}</h1>;
}

export function DashboardOverview() {
  const recipes = useMyRecipes();
  const items = recipes.data ?? [];
  const gaps = useDraftGaps(items);

  if (recipes.isPending) return <Skeleton width="100%" height="6rem" />;

  const summary = summarizeMyRecipes(items);
  const incompleteDrafts = countMissingIngredients(gaps);
  const todo = items
    .filter((recipe) => recipe.status === "Draft")
    .map((recipe) => ({ recipe, note: draftNote(gaps.has(recipe.id) ? gaps.get(recipe.id) : undefined, Boolean(recipe.thumbnailUrl)) }))
    .filter((item) => item.note !== null)
    .slice(0, TODO_COUNT);
  const recent = [...items].sort((a, b) => lastTouched(b).localeCompare(lastTouched(a))).slice(0, ACTIVITY_COUNT);

  return (
    <>
      <section className="dashboard-stats" aria-label="Tổng quan">
        <Link href={`${routes.dashboardRecipes}?status=draft`} className="rise rw dashboard-stat dashboard-stat-draft">
          <span>Bản nháp</span><strong>{summary.draft}</strong><small>{incompleteDrafts > 0 ? `${incompleteDrafts} bản thiếu nguyên liệu` : "Chỉ bạn nhìn thấy"}</small>
        </Link>
        <Link href={`${routes.dashboardRecipes}?status=published`} className="rise rw dashboard-stat dashboard-stat-published">
          <span>Đã xuất bản</span><strong>{summary.published}</strong><small>Hiển thị với mọi người</small>
        </Link>
        <Link href={`${routes.dashboardRecipes}?status=archived`} className="rise rw dashboard-stat dashboard-stat-archived">
          <span>Lưu trữ</span><strong>{summary.archived}</strong><small>Đã ẩn khỏi trang công khai</small>
        </Link>
      </section>
      <div className="dashboard-panels">
        <section className="dashboard-panel" aria-labelledby="dashboard-todo-title">
          <h2 id="dashboard-todo-title">Cần hoàn thiện</h2>
          {todo.length === 0 ? (
            <p className="dashboard-panel-empty">Không có bản nháp nào đang chờ.</p>
          ) : (
            <ul>
              {todo.map(({ recipe, note }) => (
                <li className="rw dashboard-todo" key={recipe.id}>
                  <RecipeImage
                    alt=""
                    className="dish ph dashboard-todo-thumb"
                    height={60}
                    image={recipe.thumbnailUrl ? { thumbnailUrl: recipe.thumbnailUrl, mediumUrl: null, originalUrl: recipe.thumbnailUrl, url: recipe.thumbnailUrl } : null}
                    sizes="60px"
                    variant="thumbnail"
                    width={60}
                  />
                  <span><strong>{recipe.title}</strong><small>{note}</small></span>
                  <Link className="btn btn-outline" href={`${routes.dashboardRecipes}/${recipe.id}/edit`}>Tiếp tục sửa</Link>
                </li>
              ))}
            </ul>
          )}
        </section>
        <section className="dashboard-panel" aria-labelledby="dashboard-recent-title">
          <h2 id="dashboard-recent-title">Hoạt động gần đây</h2>
          {recent.length === 0 ? (
            <p className="dashboard-panel-empty">Bạn chưa có công thức nào.</p>
          ) : (
            <ol>
              {recent.map((recipe) => (
                <li className="dashboard-activity" key={recipe.id}>
                  <span aria-hidden="true" style={{ background: activity[recipe.status].color }} />
                  <span>
                    <span><strong>{activity[recipe.status].label}</strong> · {recipe.title}</span>
                    <small>{relativeTimeVi(lastTouched(recipe))}</small>
                  </span>
                </li>
              ))}
            </ol>
          )}
          <Link href={routes.dashboardRecipes} className="dashboard-link">Xem tất cả công thức</Link>
        </section>
      </div>
    </>
  );
}
