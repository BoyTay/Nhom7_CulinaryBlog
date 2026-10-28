import type { Metadata } from "next";
import { MyRecipesTable } from "@/components/dashboard/my-recipes-table";

export const metadata: Metadata = {
  title: "Công thức của tôi",
  robots: { index: false, follow: false },
};

export default function MyRecipesPage() {
  return (
    <>
      <h1>Công thức của tôi</h1>
      <p className="dashboard-lede">Xuất bản, lưu trữ hoặc xóa các công thức bạn đã viết.</p>
      <MyRecipesTable />
    </>
  );
}
