import { Alert } from "@/components/shared";
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
      <section className="recipe-list-empty" aria-live="polite">
        <h2>Không có công thức phù hợp</h2>
        <p>Thử bỏ bớt bộ lọc hoặc chọn độ khó khác.</p>
      </section>
    );
  }

  return (
    <section className="recipe-list-grid" aria-label="Danh sách công thức">
      {model.cards.map((card) => <RecipeCard card={card} key={card.id} />)}
    </section>
  );
}
