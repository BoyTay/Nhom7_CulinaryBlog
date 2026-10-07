import type { Metadata } from "next";
import CategoryManager from "./category-manager";

export const metadata: Metadata = { title: "Quản lý danh mục" };

export default function AdminCategoriesPage() {
  return <CategoryManager />;
}
