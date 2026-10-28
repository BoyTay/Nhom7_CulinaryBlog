import type { Metadata } from "next";
import { DashboardOverview } from "@/components/dashboard/dashboard-overview";

export const metadata: Metadata = {
  title: "Tổng quan",
  robots: { index: false, follow: false },
};

export default function DashboardPage() {
  return (
    <>
      <h1>Tổng quan</h1>
      <p className="dashboard-lede">Theo dõi các công thức bạn đã chia sẻ.</p>
      <DashboardOverview />
    </>
  );
}
