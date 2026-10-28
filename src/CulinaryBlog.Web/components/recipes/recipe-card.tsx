import Link from "next/link";
import type { RecipeCardModel } from "@/lib/recipes/list";
import { RecipeImage } from "./recipe-image";
import "./recipe-card.css";

function ClockIcon() {
  return (
    <svg aria-hidden="true" width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
      <circle cx="12" cy="12" r="9" />
      <path d="M12 7v5l3 2" />
    </svg>
  );
}

/** The recipe card used on the home page, lists and category pages. Headings stay h3 so pages own their h1/h2. */
export function RecipeCard({ card }: { card: RecipeCardModel }) {
  return (
    <Link className="card recipe-card" href={card.href}>
      <RecipeImage
        alt={card.thumbnailAlt}
        className="recipe-card-image"
        height={170}
        image={card.thumbnailUrl ? { thumbnailUrl: card.thumbnailUrl, mediumUrl: null, originalUrl: card.thumbnailUrl, url: card.thumbnailUrl } : null}
        sizes="(max-width: 767px) 96px, 280px"
        variant="thumbnail"
        width={320}
      />
      <div className="recipe-card-body">
        <span className="recipe-card-category">{card.categoryName}</span>
        <h3>{card.title}</h3>
        <div className="recipe-card-meta">
          <span><ClockIcon />{card.cookTimeLabel}</span>
          <span>{card.difficultyLabel}</span>
        </div>
      </div>
    </Link>
  );
}
