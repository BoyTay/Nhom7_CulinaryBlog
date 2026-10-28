import { permanentRedirect } from "next/navigation";
import { routes } from "@/lib/navigation/routes";

export default function LegacyAdminCategoriesPage() {
  permanentRedirect(routes.dashboardCategories);
}
