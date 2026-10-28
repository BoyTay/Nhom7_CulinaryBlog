import type { Metadata } from "next";
import Link from "next/link";
import { Suspense } from "react";
import { DashboardOverview } from "@/components/dashboard/dashboard-overview";
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
        <h1>Tổng quan</h1>
        <Link className="btn btn-primary btn-square" href={routes.dashboardRecipeNew}>Tạo công thức mới</Link>
      </div>
      <p className="dashboard-lede">Theo dõi các công thức bạn đã chia sẻ.</p>
      <Suspense fallback={<Skeleton width="100%" height="6rem" />}>
        <DashboardOverview />
      </Suspense>
    </>
  );
}
