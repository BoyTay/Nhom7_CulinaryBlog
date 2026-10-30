import Link from "next/link";
import { Alert } from "@/components/shared";
import { routes } from "@/lib/navigation/routes";
import type { RecipeListViewModel } from "@/lib/recipes/list";
import { RecipeCard } from "./recipe-card";

export function RecipeListResults({ model }: { model: RecipeListViewModel }) {
  if (model.state === "error") {
    return (
      <Alert variant="error" title="Chưa thể tải công thức">
        Máy chủ đang gặp sự cố. Vui lòng thử lại sau.
      </Alert>
    );
  }

  if (model.state === "empty") {
    return (
      <section className="empty-state" aria-live="polite">
        <span className="empty-state-icon">
          <svg aria-hidden="true" width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round"><circle cx="11" cy="11" r="7" /><path d="M20 20l-3.5-3.5" /></svg>
        </span>
        <h2>Chưa có công thức nào khớp bộ lọc</h2>
        <p>Thử nới thời gian nấu hoặc bỏ bớt một điều kiện để thấy thêm món.</p>
        <Link className="btn btn-primary" href={routes.recipes}>Xóa bộ lọc</Link>
      </section>
    );
  }

  return (
    <section className="recipe-list-grid" aria-label="Danh sách công thức">
      {model.cards.map((card) => <RecipeCard card={card} key={card.id} />)}
    </section>
  );
}
