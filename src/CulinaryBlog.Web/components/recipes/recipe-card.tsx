import Link from "next/link";
import type { RecipeDifficulty } from "@/lib/recipes/contracts";
import type { RecipeCardModel } from "@/lib/recipes/list";
import { RecipeImage } from "./recipe-image";
import "./recipe-card.css";

export const difficultyBadgeClass: Record<RecipeDifficulty, string> = {
  Easy: "badge badge-easy",
  Medium: "badge badge-medium",
  Hard: "badge badge-hard",
};

export function ClockIcon() {
  return (
    <svg aria-hidden="true" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
      <circle cx="12" cy="12" r="9" />
      <path d="M12 7v5l3 2" />
    </svg>
  );
}

/** The recipe card used on the home page, lists and category pages. Headings stay h3 so pages own their h1/h2. */
export function RecipeCard({ card }: { card: RecipeCardModel }) {
  return (
    <Link className="card recipe-card" href={card.href}>
      <div className="recipe-card-photo">
        <span className="recipe-card-category">{card.categoryName}</span>
        <RecipeImage
          alt={card.thumbnailAlt}
          className="recipe-card-image dish ph"
          height={240}
          image={card.thumbnailUrl ? { thumbnailUrl: card.thumbnailUrl, mediumUrl: null, originalUrl: card.thumbnailUrl, url: card.thumbnailUrl } : null}
          sizes="(max-width: 767px) 100vw, 280px"
          variant="medium"
          width={320}
        />
      </div>
      <div className="recipe-card-body">
        <h3>{card.title}</h3>
        {card.description && <p>{card.description}</p>}
        <div className="recipe-card-meta">
          <span><ClockIcon />{card.cookTimeLabel}</span>
          <span className={difficultyBadgeClass[card.difficulty]}>{card.difficultyLabel}</span>
        </div>
      </div>
    </Link>
  );
}
