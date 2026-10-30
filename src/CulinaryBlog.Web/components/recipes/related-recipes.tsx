import Link from "next/link";
import type { RecipeCardModel } from "@/lib/recipes/list";
import { RecipeImage } from "./recipe-image";

export function RelatedRecipes({ cards }: { cards: RecipeCardModel[] }) {
  if (cards.length === 0) return null;

  return (
    <section className="recipe-detail-related" aria-labelledby="related-heading">
      <h2 id="related-heading">Có thể bạn cũng thích</h2>
      <div className="recipe-detail-related-grid">
        {cards.map((card) => (
          <Link className="card recipe-related-card" href={card.href} key={card.id}>
            <RecipeImage
              alt={card.thumbnailAlt}
              className="dish recipe-related-image"
              height={96}
              image={card.thumbnailUrl ? { thumbnailUrl: card.thumbnailUrl, mediumUrl: null, originalUrl: card.thumbnailUrl, url: card.thumbnailUrl } : null}
              sizes="96px"
              variant="thumbnail"
              width={96}
            />
            <span>
              <strong>{card.title}</strong>
              <small>{card.categoryName} · {card.cookTimeLabel}</small>
            </span>
          </Link>
        ))}
      </div>
    </section>
  );
}
