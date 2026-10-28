import Link from "next/link";
import { Alert } from "@/components/shared";

export default function RecipeNotFound() {
  return (
    <div className="recipe-detail-page">
      <main className="recipe-detail-state">
        <Alert
          variant="warning"
          title="Không tìm thấy công thức"
          action={<Link className="recipe-detail-state-action" href="/recipes">Xem công thức khác</Link>}
        >
          Công thức không tồn tại, chưa được xuất bản hoặc không còn công khai.
        </Alert>
      </main>
    </div>
  );
}
