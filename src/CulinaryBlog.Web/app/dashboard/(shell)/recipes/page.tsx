import type { Metadata } from "next";
import Link from "next/link";
import { Suspense } from "react";
import { MyRecipesTable } from "@/components/dashboard/my-recipes-table";
import { Skeleton } from "@/components/shared";
import { routes } from "@/lib/navigation/routes";

export const metadata: Metadata = {
  title: "Công thức của tôi",
  robots: { index: false, follow: false },
};

export default function MyRecipesPage() {
  return (
    <>
      <div className="dashboard-title-row">
        <h1>Công thức của tôi</h1>
        <Link className="btn btn-primary btn-square" href={routes.dashboardRecipeNew}>Tạo công thức mới</Link>
      </div>
      <Suspense fallback={<Skeleton width="100%" height="3rem" />}>
        <MyRecipesTable />
      </Suspense>
    </>
  );
}
