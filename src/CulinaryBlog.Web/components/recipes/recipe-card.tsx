import Link from "next/link";
import type { RecipeListCardModel } from "@/lib/recipes/list";
import { RecipeImage } from "./recipe-image";

export function RecipeCard({ card }: { card: RecipeListCardModel }) {
  return (
    <Link className="recipe-list-card" href={card.href}>
      <RecipeImage
        alt={`Chưa có ảnh minh họa cho ${card.title}`}
        className="recipe-list-card-placeholder"
        height={160}
        width={320}
      />
      <div className="recipe-list-card-body">
        <h2>{card.title}</h2>
        <div className="recipe-list-card-meta">
          <span>{card.cookTimeLabel}</span>
          <span>{card.difficultyLabel}</span>
        </div>
      </div>
    </Link>
  );
}
