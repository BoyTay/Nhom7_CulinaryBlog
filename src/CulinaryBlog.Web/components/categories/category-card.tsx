import Link from "next/link";
import "./category-card.css";

export function CategoryCard({
  name,
  href,
  description,
  recipeCount,
}: {
  name: string;
  href: string;
  description: string | null;
  recipeCount: number;
}) {
  return (
    <Link className="card category-card" href={href}>
      <span className="category-card-strip" aria-hidden="true" />
      <h2>{name}</h2>
      <p>{description?.trim() || "Chưa có mô tả."}</p>
      <span className="category-card-count">{recipeCount} công thức</span>
    </Link>
  );
}
