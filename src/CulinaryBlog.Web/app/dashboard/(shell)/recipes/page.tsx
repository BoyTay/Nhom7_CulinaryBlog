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
        <div>
          <h1>Công thức của tôi</h1>
          <p className="dashboard-lede">Soạn nháp, xuất bản và quản lý các công thức bạn đã viết.</p>
        </div>
        <Link className="btn btn-primary dashboard-new" href={routes.dashboardRecipeNew}><svg aria-hidden="true" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.4" strokeLinecap="round"><path d="M12 5v14M5 12h14" /></svg>Viết công thức mới</Link>
      </div>
      <Suspense fallback={<Skeleton width="100%" height="3rem" />}>
        <MyRecipesTable />
      </Suspense>
    </>
  );
}
