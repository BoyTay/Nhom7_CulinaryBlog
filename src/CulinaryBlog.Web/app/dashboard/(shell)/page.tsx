import type { Metadata } from "next";
import Link from "next/link";
import { Suspense } from "react";
import { DashboardGreeting, DashboardOverview } from "@/components/dashboard/dashboard-overview";
import { Skeleton } from "@/components/shared";
import { routes } from "@/lib/navigation/routes";

export const metadata: Metadata = {
  title: "Tổng quan",
  robots: { index: false, follow: false },
};

export default function DashboardPage() {
  return (
    <>
      <div className="dashboard-title-row">
        <div>
          <DashboardGreeting />
          <p className="dashboard-lede">Đây là tình hình các công thức của bạn hôm nay.</p>
        </div>
        <Link className="btn btn-primary dashboard-new" href={routes.dashboardRecipeNew}>Viết công thức mới</Link>
      </div>
      <Suspense fallback={<Skeleton width="100%" height="6rem" />}>
        <DashboardOverview />
      </Suspense>
    </>
  );
}
