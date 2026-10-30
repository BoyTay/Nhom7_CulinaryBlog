import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { ProtectedRoute } from "@/components/auth/protected-route";
import { RecipeEditor } from "@/components/recipes/editor/recipe-editor";
import { isRecipeId } from "@/lib/recipes/detail";
import { RECIPE_EDITOR_ROLES, initialEditorStep } from "@/lib/recipes/editor";

export const metadata: Metadata = {
  title: "Chỉnh sửa công thức",
  robots: { index: false, follow: false },
};

export default async function EditRecipePage({
  params,
  searchParams,
}: {
  params: Promise<{ id: string }>;
  searchParams: Promise<{ created?: string }>;
}) {
  const { id } = await params;
  const { created } = await searchParams;
  if (!isRecipeId(id)) notFound();

  const initialNotice = created === "partial"
    ? "Bản nháp đã được tạo, nhưng lần lưu dinh dưỡng đầu tiên chưa hoàn tất. Hãy kiểm tra và lưu lại."
    : created === "1"
      ? "Đã tạo bản nháp. Bạn có thể thêm ảnh ngay bây giờ."
      : undefined;

  return (
    <ProtectedRoute requiredRoles={[...RECIPE_EDITOR_ROLES]}>
      <RecipeEditor initialNotice={initialNotice} initialStep={initialEditorStep(created)} mode="edit" recipeId={id} />
    </ProtectedRoute>
  );
}
