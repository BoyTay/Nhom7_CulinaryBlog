import Link from "next/link";
import { Alert } from "@/components/shared";
import { routes } from "@/lib/navigation/routes";
import "../../recipes/recipes.css";

export default function CategoryNotFound() {
  return (
    <div className="recipes-page ds-scope">
      <main className="recipe-list-main">
        <Alert variant="warning" title="Không tìm thấy danh mục" action={<Link href={routes.categories}>Xem tất cả danh mục</Link>}>
          Danh mục này không tồn tại hoặc đã bị xóa.
        </Alert>
      </main>
    </div>
  );
}
