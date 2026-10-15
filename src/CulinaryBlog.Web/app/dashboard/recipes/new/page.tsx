import type { Metadata } from "next";
import { ProtectedRoute } from "@/components/auth/protected-route";
import { RecipeEditor } from "@/components/recipes/editor/recipe-editor";
import { RECIPE_EDITOR_ROLES } from "@/lib/recipes/editor";

export const metadata: Metadata = {
  title: "Tạo công thức mới",
  robots: { index: false, follow: false },
};

export default function NewRecipePage() {
  return (
    <ProtectedRoute requiredRoles={[...RECIPE_EDITOR_ROLES]}>
      <RecipeEditor mode="create" />
    </ProtectedRoute>
  );
}
