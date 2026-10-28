import type { RecipeCardModel } from "@/lib/recipes/list";
import { RecipeCard } from "./recipe-card";

export function RelatedRecipes({ categoryName, cards }: { categoryName: string; cards: RecipeCardModel[] }) {
  if (cards.length === 0) return null;

  return (
    <section className="recipe-detail-related" aria-labelledby="related-heading">
      <h2 id="related-heading">Cùng danh mục {categoryName}</h2>
      <div className="recipe-detail-related-grid">
        {cards.map((card) => <RecipeCard card={card} key={card.id} />)}
      </div>
    </section>
  );
}
