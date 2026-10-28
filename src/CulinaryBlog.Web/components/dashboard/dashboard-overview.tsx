"use client";

import Link from "next/link";
import { Skeleton } from "@/components/shared";
import { summarizeMyRecipes } from "@/lib/dashboard/my-recipes";
import { routes } from "@/lib/navigation/routes";
import { useMyRecipes } from "./my-recipes-table";

export function DashboardOverview() {
  const recipes = useMyRecipes();

  if (recipes.isPending) return <Skeleton width="100%" height="6rem" />;

  const summary = summarizeMyRecipes(recipes.data ?? []);

  return (
    <>
      <div className="dashboard-stats" role="group" aria-label="Thống kê công thức">
        <div className="dashboard-stat"><strong>{summary.published}</strong><span>Đã xuất bản</span></div>
        <div className="dashboard-stat"><strong>{summary.draft}</strong><span>Bản nháp</span></div>
        <div className="dashboard-stat"><strong>{summary.archived}</strong><span>Đã lưu trữ</span></div>
      </div>
      <p><Link href={routes.dashboardRecipes}>Quản lý công thức của tôi →</Link></p>
    </>
  );
}
